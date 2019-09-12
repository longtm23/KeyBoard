using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanPhimCung.Command;
using BanPhimCung.Connect_Socket;
using BanPhimCung.DTO;
using BanPhimCung.Ultility;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;
using System.Timers;
using BanPhimCung.Controller;
using System.Runtime.InteropServices;

namespace BanPhimCung
{
    class Program
    {
        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);
        #region PARAM
        static string comName = "";
        static readonly SocketLib socket = SocketLib.Instance;
        static ConfigKeyBoard config = null;
        static Dictionary<string, Service> dicService = null;
        static Dictionary<string, Counter> dicCounter = null;
        static Dictionary<int, KeyBoardCounter> dicCounterKeyboard = null;
        static readonly SerialSocketController serialCtrl = SerialSocketController.Instance;
        #endregion PARAM

        #region Init
        private static void Init(string comName)
        {
            InitClass(comName);
            getConfigOpenSocket();
        }

        private static System.Timers.Timer aTimePing;
        private static void SetTimerPing()
        {
            // Create a timer with a two second interval.
            if (aTimePing == null)
            {
                aTimePing = new System.Timers.Timer(5000);
                // Hook up the Elapsed event for the timer. 
                aTimePing.Elapsed += OnTimedPingEvent;
                aTimePing.AutoReset = true;
                aTimePing.Enabled = true;
            }
            else if (!aTimePing.Enabled)
            {
                aTimePing.Enabled = true;
            }
        }

        private static void checkOpenConnect()
        {
            KeyBoardCtrl keyCtrl = new KeyBoardCtrl();
            if (keyCtrl.CheckConfig(config.https, config.StoreCode))
            {
                if (getConfigOpenSocket())
                {
                    aTimePing.Enabled = false;
                    aTimePing = null;
                }
            }
        }
        private static void OnTimedPingEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            checkOpenConnect();
        }
        private static bool getConfigOpenSocket()
        {
            KeyBoardCtrl keyCtrl = new KeyBoardCtrl();
            Priority priority = null;
            try
            {
                var dataConfig = keyCtrl.GetConfig(config.https, config.StoreCode);

                if (dataConfig != null)
                {
                    if (dataConfig.priority_fix != null)
                    {
                        priority = dataConfig.priority_fix;
                    }
                    else
                    {
                        priority = dataConfig.priority;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                foreach (var add in config.KeyBoardCounters)
                {
                    EventProgram evDis = new EventProgram(ActionTicket.DISCONNECT, add.AddressKeyboard, null);
                    serialCtrl.SendKeyBoard(evDis);
                }
                SetTimerPing();
                return false;
            }

            InitSocket(config, priority);
            return true;

        }
        private static void InitClass(string comName)
        {
            dicCounter = new Dictionary<string, Counter>();
            dicService = new Dictionary<string, Service>();
            dicCounterKeyboard = new Dictionary<int, KeyBoardCounter>();
            serialCtrl.InitSerialSocketController(comName);
            serialCtrl.DataReceived += ReciveSerialCtrl;
        }

        private static void ReciveSerialCtrl(EventProgram ev)
        {
            socket.SendSocket(ev.Action, ev.CounterID, ev.LstCounter, ev.LstServices, ev.CNum, ev.PointRate);
        }
        private static void InitSocket(ConfigKeyBoard config, Priority pri)
        {
            //socket = new SocketLib(config, pri);
            socket.InitSocketLib(config, pri);
            socket.DataReceived += ReciveDataSocket;
        }
        private static void InitDic(Dictionary<string, Service> dicSer, Dictionary<string, Counter> dicCount)
        {
            dicService = dicSer;
            dicCounter = dicCount;
        }
        #endregion

        #region LẤY DATA KHI JOIN SOCKET SET CHO DEVICE
        private static void InitKeyBoardSend(EventSocket eventData)
        {
            if (eventData.IsLoadService)
            {
                dicService = sortDicServices(eventData.DicService);
                List<string> lstServices = dicService.Values.OrderBy(m => m.NameService).Select(m => m.NameService).ToList();
                EventProgram ev = new EventProgram(ActionTicket.INITIAL_SER, null, lstServices);
                serialCtrl.SendKeyBoard(ev);
                Thread.Sleep(5000);
            }

            #region SET SERVICE COUNTER CHO DEVICE
            // send Counter
            if (eventData.IsLoadCounter)
            {
                dicCounter = sortDicCounter(eventData.DicCounter);
                foreach (var item in config.KeyBoardCounters)
                {
                    int address = item.AddressKeyboard;
                    int numCounter = item.NumCounter;

                    var cou = dicCounter.Values.FirstOrDefault(m => m.CNum == numCounter);
                    if (cou != null && !string.IsNullOrWhiteSpace(cou.Id))
                    {
                        item.CounterID = cou.Id;
                        dicCounterKeyboard[address] = item;
                    }

                }
            }
            List<string> lstCounters = dicCounter.Values.Select(m => m.Name).ToList();
            //EventProgram evCou = new EventProgram(ActionTicket.INITIAL_COU, 1, lstCounters);// fix 1 = mấy đều được
            //serialCtrl.SendKeyBoard(evCou);
            EventProgram evCou1 = new EventProgram(ActionTicket.INITIAL_COU, eventData.DicCounter.Values.OrderBy(m => m.CNum).ToList(), dicCounterKeyboard);// fix 1 = mấy đều được
            serialCtrl.SendKeyBoard(evCou1);
            Thread.Sleep(5000);
            //Set lai data cho recive port
            serialCtrl.SetDataRecive(dicService, dicCounter, dicCounterKeyboard);
            Thread.Sleep(1800);
            #endregion
            //send Led inital

            var evReset = new EventProgram(ActionTicket.ACTION_RESET);
            serialCtrl.SendKeyBoard(evReset);
            Thread.Sleep(100);
            SendNumToCounter(eventData, false);

            Console.WriteLine("Set services and counter success!");
        }

        #region SEND NUM ĐANG PHỤC VỤ XUỐNG QUẦY
        private static void SendNumToCounter(EventSocket eventData, bool isOpened)
        {
            bool isLst = false;
            if (eventData.LstSend != null && eventData.LstSend.Count() > 0)
            {
                isLst = true;
                foreach (var obj in eventData.LstSend)
                {
                    var addKeyBoard = dicCounterKeyboard.Values.FirstOrDefault(m => m.CounterID == obj.Counter_Id);
                    if (addKeyBoard != null)
                    {
                        int address = addKeyBoard.AddressKeyboard;
                        if (address != 0)
                        {
                            var counterID = addKeyBoard.CounterID;

                            var indexService = getIndexService(socket.GetService(obj), counterID);
                            var data = obj.CNum;
                            EventProgram ev = new EventProgram("call_hst", address, counterID, data, indexService);
                            serialCtrl.SendKeyBoard(ev);
                            Thread.Sleep(1000);
                            serialCtrl.SendLED(eventData.Action, address, data);
                        }

                    }
                }
            }
            if (isOpened && isLst)
            {
                var lstCounterId = eventData.LstSend.Select(m => m.Counter_Id);
                var dicOut = dicCounterKeyboard.Where(m => !lstCounterId.Contains(m.Value.CounterID));
                if (dicOut != null && dicOut.Count() > 0)
                {
                    foreach (var item in dicOut)
                    {
                        EventProgram ev = new EventProgram(ActionTicket.MESSAGE_ERROR, item.Key, item.Value.CounterID, MessageError.ERROR_09, 0);
                        serialCtrl.SendKeyBoard(ev);
                    }
                }
            }

        }


        #endregion

        #endregion

        #region NHẬN DATA SOCKET TRUYỀN CHO DEVICE
        private static void ReciveDataSocket(EventSocket eventData)
        {
            if (eventData == null)
            {
                Console.WriteLine("Lỗi event");
                return;
            }
            string data = null;
            string counterOld = eventData.CounterID;

            if (string.IsNullOrWhiteSpace(eventData.Message))
            {
                string action = eventData.Action;
                switch (action)
                {
                    case ActionTicket.INITIAL:
                        InitKeyBoardSend(eventData);
                        return;
                    case ActionTicket.OPEN_SERVER:
                        SendNumToCounter(eventData, true);
                        return;
                    case ActionTicket.ACTION_CREATE:// không làm gì
                        var address = dicCounterKeyboard.FirstOrDefault(d => d.Value.CounterID == eventData.CounterID).Key;
                        EventProgram ev = new EventProgram(ActionTicket.MESSAGE_ERROR, address, eventData.CounterID, MessageError.ERROR_00, 0);
                        serialCtrl.SendKeyBoard(ev);
                        return;
                    case ActionTicket.ACTION_CALL:
                        sendActionToKeyBoard(action, eventData.TkServing, eventData.CounterID);
                        break;
                    case ActionTicket.ACTION_RECALL:
                        sendActionToKeyBoard(action, eventData.TkServing, eventData.CounterID);
                        break;
                    case ActionTicket.ACTION_RESTORE:
                        sendActionToKeyBoard(action, eventData.TkServing, eventData.CounterID);
                        break;
                    case ActionTicket.ACTION_MOVE_COUNTER:
                        sendActionToKeyBoard(action, eventData.TkServing, eventData.CounterID);
                        break;
                    case ActionTicket.ACTION_MOVE_SERVICE:
                        sendActionToKeyBoard(action, eventData.TkServing, eventData.CounterID);
                        break;
                    case ActionTicket.ACTION_CANCEL:

                        sendActionToKeyBoard(action, eventData.TkServing, eventData.CounterID);
                        break;
                    case ActionTicket.ACTION_FINISH:
                        sendActionToKeyBoard(action, eventData.TkServing, eventData.CounterID);
                        break;
                    case ActionTicket.ACTION_ALL_RESTORE:
                        SendDataRetore(action, counterOld, eventData.LstSend, dicService);
                        break;
                    case ActionTicket.CALL_LIST_WATTING:
                        SendDataRetore(action, counterOld, eventData.LstSend, dicService);
                        break;
                    case ActionTicket.ACTION_CONNECT_COUNTER:
                        sendActionToKeyBoard(action, eventData.TkServing, eventData.CounterID);
                        break;
                    case ActionTicket.ACTION_PING:
                        serialCtrl.SendLED(action, 0, data);
                        break;
                    case ActionTicket.DISCONNECT:
                        EventProgram evDis = new EventProgram(ActionTicket.DISCONNECT, 0, null);
                        serialCtrl.SendKeyBoard(evDis);
                        return;
                    case ActionTicket.CONNECT:
                        EventProgram evCon = new EventProgram(ActionTicket.CONNECT, 0, null);
                        serialCtrl.SendKeyBoard(evCon);
                        return;
                    default:
                        //bug
                        break;
                }
            }
            else
            {
                var address = dicCounterKeyboard.FirstOrDefault(d => d.Value.CounterID == counterOld).Key;
                if (address == 0)
                {
                    return;
                }
                EventProgram ev = new EventProgram(ActionTicket.MESSAGE_ERROR, address, eventData.CounterID, eventData.Message, 1);
                serialCtrl.SendKeyBoard(ev);
            }
        }

        private static void sendActionToKeyBoard(string action, Ticket tk, string counterOld)
        {
            if (tk == null)
            {
                Console.WriteLine("Không có ticket");
                return;
            }
            string data = tk.CNum;
            var address = dicCounterKeyboard.FirstOrDefault(d => d.Value.CounterID == counterOld).Key;
            if ((ActionTicket.ACTION_MOVE_SERVICE.Equals(action) || ActionTicket.ACTION_MOVE_COUNTER.Equals(action)) && tk != null)
            {

                var index = -1;
                if (action.Equals(ActionTicket.ACTION_MOVE_COUNTER)) index = getIndexCounter(counterOld, dicCounter);
                else
                {
                    var serviceID = socket.GetService(tk);
                    index = getIndexService(serviceID, counterOld);
                }

                EventProgram ev = new EventProgram(action, address, counterOld, data, index);
                serialCtrl.SendKeyBoard(ev);
            }
            else
            {
                var serviceID = socket.GetService(tk);
                SendDataToPortHardWear(tk.Id, counterOld, serviceID, data, action);
            }
            serialCtrl.SendLED(action, address, data);
        }
        #endregion

        #region SERIALPORT NHẬN DATA GỬI LÊN SOCKET
        static void SendDataRetore(string action, string counterID, List<Ticket> lstObject, Dictionary<string, Service> dicSer)
        {
            if (lstObject != null && lstObject.Count() > 0)
            {
                var address = dicCounterKeyboard.FirstOrDefault(d => d.Value.CounterID == counterID).Key;
                if (address > 0)
                {
                    EventProgram ev = new EventProgram(action, address, lstObject, dicSer);
                    serialCtrl.SendKeyBoard(ev);
                }
            }
            else
            {
                Console.WriteLine(MessageError.ERROR_03);
            }
        }

        static void SendDataToPortHardWear(string ticketID, string counterID, string serviceID, string data, string action)
        {
            var address = dicCounterKeyboard.FirstOrDefault(d => d.Value.CounterID == counterID).Key;
            if (ticketID != null || !"".Equals(ticketID))
            {
                int indexService = -1;

                if (action.Equals(ActionTicket.ACTION_FINISH))
                {
                    indexService = 0;
                    data = "STOP";
                }
                else indexService = getIndexService(serviceID, counterID);

                if ((indexService != -1) && (address > 0))
                {
                    EventProgram ev = new EventProgram(action, address, counterID, data, indexService);
                    serialCtrl.SendKeyBoard(ev);
                }
                else
                {
                    Console.WriteLine(MessageError.ERROR_03);
                }
            }
            else
            {
                EventProgram ev = new EventProgram(action, address, counterID, MessageError.ERROR_01, 0);
                serialCtrl.SendKeyBoard(ev);
            }
        }
        #endregion SERIALPORT NHẬN DATA GỬI LÊN SOCKET
        #region LẤY CỔNG COM AND SET ĐIỀU KIỆN DEVICE
        private static string GetCom(string comName)
        {
            try
            {
                if (comName != null && comName.Equals(""))
                {
                    string[] ports = SerialPort.GetPortNames();
                    if (ports.Count() == 1) comName = ports[0];
                    else
                    {
                        Console.WriteLine("missing com port");
                        return null;
                    }
                }
                comName = comName.ToUpper();
            }
            catch (Exception e)
            {
                Console.WriteLine("cắm COM or chọn cổng COM" + e.Message);
                Console.ReadLine();
            }
            return comName;

        }

        #endregion
        static void GetInitSetCache()
        {
            config = MRW_Common.GetConfigByFileConfig();
            if (config != null)
            {
                hideConsole(config.Hide);
            }
            comName = GetCom(config.Com.Trim());

        }
        static void Conso()
        {
            string a = Console.ReadLine();
            if (a.Equals("MRW-STOP"))
            {
                return;
            }
            else
            {
                Conso();
            }
        }
        static void hideConsole(bool isHide)
        {
            try
            {
                if (isHide)
                {
                    IntPtr hw = GetConsoleWindow();
                    if (hw != null && hw != IntPtr.Zero)
                    {
                        ShowWindow(hw, 0);
                    }
                }
            }
            catch { }
        }
        static void Main(string[] args)
        {
            
            GetInitSetCache();
            if (comName.Length > 0)
            {
                try
                {
                    Init(comName);
                    Conso();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("exception " + ex);
                }
            }
            return;
        }

        static Dictionary<string, Service> sortDicServices(IDictionary<string, Service> dic)
        {
            var list = dic.ToList();
            list.Sort((pair1, pair2) => pair1.Value.NameService.CompareTo(pair2.Value.NameService));
            return list.ToDictionary(pair => pair.Key, pair => pair.Value);
        }
        static Dictionary<string, Counter> sortDicCounter(IDictionary<string, Counter> dic)
        {
            var list = dic.ToList();
            list.Sort((pair1, pair2) => pair1.Value.CNum.CompareTo(pair2.Value.CNum));
            return list.ToDictionary(pair => pair.Key, pair => pair.Value);
        }
        private static int getIndexService(string serviceID, string counterID)
        {
            if (counterID != null && serviceID != null && dicCounter.ContainsKey(counterID) && dicService.ContainsKey(serviceID))
            {
                var index = Array.FindIndex(dicService.Values.ToArray(), m => m.Id.Contains(serviceID));
                return index;
            }
            return -1;
        }
        private static int getIndexCounter(string counterID, Dictionary<string, Counter> dicCou)
        {
            if (counterID != null && dicCou.ContainsKey(counterID))
            {
                var index = Array.FindIndex(dicCou.Keys.ToArray(), m => m.Contains(counterID));
                return index;
            }
            return -1;
        }
    }
    public class KeyAddressDevice : EventArgs
    {
        private int deviceId;
        private int addressDevice;
        private string idCounter;
        private int commandFromKeyboard;

        public int CommandFromKeyboard
        {
            get { return commandFromKeyboard; }
            set { commandFromKeyboard = value; }
        }

        public string IdCounter
        {
            get { return idCounter; }
            set { idCounter = value; }
        }
        public int DeviceId
        {
            get { return deviceId; }
            set { deviceId = value; }
        }
        public int AddressDevice
        {
            get { return addressDevice; }
            set { addressDevice = value; }
        }

        public KeyAddressDevice(int deviceID, int address, string counterID, int commandFromKeyboard)
        {
            this.DeviceId = deviceId;
            this.AddressDevice = address;
            this.IdCounter = counterID;
            this.CommandFromKeyboard = commandFromKeyboard;
        }
        public KeyAddressDevice() { }
    }
}
