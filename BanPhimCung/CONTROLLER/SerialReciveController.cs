using BanPhimCung.Command;
using BanPhimCung.DTO;
using BanPhimCung.Ultility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace BanPhimCung.Controller
{
    public class SerialReciveController
    {
        private Queue<byte[]> queueRecive = null;
        private readonly BackgroundWorker comSendWorker = new BackgroundWorker();
        public delegate void ReceivedEventHandler(EventProgram ev);
        Dictionary<int, KeyBoardCounter> dicCounterKeyboard = null;
        Dictionary<string, Counter> dicCounter = null;
        Dictionary<string, Service> dicService = null;
        public event ReceivedEventHandler DataReceived;

        private readonly Mutex mRecive = new Mutex();
        public SerialReciveController()
        {
            //contructor queue
            queueRecive = new Queue<byte[]>();
            //contructor backGround
           // comSendWorker.WorkerSupportsCancellation = true;
            comSendWorker.DoWork += comSendWorker_DoWork;
            comSendWorker.RunWorkerCompleted += readRunWorkerCompleted;
            comSendWorker.RunWorkerAsync();
        }

        private void readRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                Thread.Sleep(100);
                if (!comSendWorker.IsBusy)
                {
                    comSendWorker.RunWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        public void SetData(Dictionary<int, KeyBoardCounter> dicCouKeyboard, Dictionary<string, Counter> dicCou, Dictionary<string, Service> dicSer)
        {
            this.dicCounterKeyboard = dicCouKeyboard;
            this.dicCounter = dicCou;
            this.dicService = dicSer;
        }
        private bool checkQueue()
        {
            bool isCheck = false;
            mRecive.WaitOne();
            if (queueRecive != null && queueRecive.Count > 0)
            {
                isCheck = true;
            }
            mRecive.ReleaseMutex();
            return isCheck;
        }

        public void Recive(byte[] dataRecive)
        {

            mRecive.WaitOne();
            queueRecive.Enqueue(dataRecive);
            mRecive.ReleaseMutex();
            try
            {
                if (!comSendWorker.IsBusy)
                {
                    comSendWorker.RunWorkerAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        void comSendWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!comSendWorker.CancellationPending && checkQueue())
            {
                mRecive.WaitOne();
                var data = queueRecive.Dequeue();
                mRecive.ReleaseMutex();
                // nhận data từ bàn phím xử lý gọi lên socket
                var resBytes = CheckByteSend.getByteSendList(data, dicCounterKeyboard);
                if (resBytes != null && resBytes.Count() > 0)
                {
                    foreach (var resByte in resBytes)
                    {
                        string idCounter = "";
                        idCounter = dicCounterKeyboard[resByte.AddressKey].CounterID;

                        switch (resByte.DeviceId)
                        {
                            case (int)CheckByteSend.DEVICE_ID.DEVICE_KEYBOARD:

                                if (dicCounterKeyboard.ContainsKey(resByte.AddressKey))
                                {
                                    var arrAcNum = CheckByteSend.GetActionByCommand(resByte.Command, resByte.Cnum, resByte.NumServicesOrCounter, idCounter, dicService, dicCounter);
                                    string action = arrAcNum[0];
                                    string counterMove = arrAcNum[1];
                                    string serviceMove = arrAcNum[2];
                                    string cNumMove = arrAcNum[3];

                                    if (action == null && string.IsNullOrWhiteSpace(counterMove) && string.IsNullOrWhiteSpace(serviceMove) && string.IsNullOrWhiteSpace(cNumMove))
                                    {
                                        Console.WriteLine("Lỗi Send Action: " + MessageError.ERROR_08);
                                        EventProgram ev = new EventProgram(ActionTicket.MESSAGE_ERROR, resByte.AddressKey, idCounter, MessageError.ERROR_08, 0);
                                        DataReceived(ev);
                                    }
                                    else if (!string.IsNullOrWhiteSpace(action))
                                    {
                                        List<string> lstCou = new List<string>();
                                        List<string> lstSer = new List<string>();

                                        if (!string.IsNullOrWhiteSpace(counterMove))
                                        {
                                            lstCou = new List<string> { counterMove };
                                        }
                                        else if (!string.IsNullOrWhiteSpace(serviceMove))
                                        {
                                            lstSer = new List<string> { serviceMove };
                                        }
                                        //string action, int add, string couterID, string cNum, int indexSer
                                        EventProgram ev = new EventProgram(action, idCounter, cNumMove, lstCou, lstSer);
                                        DataReceived(ev);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Khong ton tai dia chi nay!");
                                } break;
                            case (int)CheckByteSend.DEVICE_ID.DEVICE_FEED_BACK:
                                var resByteRecive = dicCounterKeyboard.Values.FirstOrDefault(m => m.AddressFeedBack == resByte.AddressKey);
                                if (resByteRecive != null)
                                {
                                    var rating = CheckByteSend.GetRatingByCommand(resByte.Command);
                                    if (rating != 0)
                                    {
                                        EventProgram ev = new EventProgram(ActionTicket.RATING_ONCE, resByteRecive.CounterID, rating);
                                        DataReceived(ev);

                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
    }
}
