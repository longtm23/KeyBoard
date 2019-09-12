using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanPhimCung.Command;
using BanPhimCung.Connect_Socket;
using BanPhimCung.Controller;
using BanPhimCung.DTO;
using BanPhimCung.Ultility;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;
using System.Timers;


namespace BanPhimCung
{
    class Program2
    {
#region remove
      /*  #region PARAM
        static string comName = "";
        static MRW_ModBus modBus = null;
        static WriteLog log = null;
        //static Input input = null;
        static MRW_SerialPort serialPort = null;

        static SocketLib socket = null;
        static SetServiceCounter setSrcVcs = null;
        static ConfigKeyBoard config = null;
        static Dictionary<string, Service> dicService = null;
        static Dictionary<string, Counter> dicCounter = null;
        static Dictionary<int, KeyBoardCounter> dicCounterKeyboard = null;
        static List<ResponeByte> resBytes = null;
        static Queue<byte[]> sendQueueByKeyBoard = null;
        static BackgroundWorker comSendWorker;
        #region DEVICE ID
        static int DEVICE_KEYBOARD = (int)CheckByteSend.DEVICE_ID.DEVICE_KEYBOARD;
        //static int DEVICE_LED = (int)CheckByteSend.DEVICE_ID.DEVICE_LED;
        //static int DEVICE_FEEDBACK = (int)CheckByteSend.DEVICE_ID.DEVICE_FEED_BACK;
        #endregion
        #endregion PARAM

        #region Init
        private static void Init(string comName)
        {
            InitClass();
            InitSerialPort(comName); // mở port nhận và truyền data phím cứng
            KeyBoardCtrl keyCtrl = new KeyBoardCtrl();
            var dataConfig = keyCtrl.GetConfig(config.https, config.StoreCode);
            Priority priority = null;
            if (dataConfig != null)
            {
                if (dataConfig.priority != null)
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
                return;
            }
            InitSocket(config, priority);
        }
        private static void InitClass()
        {
            modBus = new MRW_ModBus();
            log = new WriteLog();
            setSrcVcs = new SetServiceCounter();
            dicCounter = new Dictionary<string, Counter>();
            dicService = new Dictionary<string, Service>();
            dicCounterKeyboard = new Dictionary<int, KeyBoardCounter>();
            sendQueueByKeyBoard = new Queue<byte[]>();

            comSendWorker = new BackgroundWorker();
            comSendWorker.WorkerSupportsCancellation = true;
            comSendWorker.DoWork += comSendWorker_DoWork;
            comSendWorker.RunWorkerAsync();
        }
        private static void InitSerialPort(string comName)
        {
            serialPort = new MRW_SerialPort(new System.IO.Ports.SerialPort(comName.ToUpper(), 19200));
            serialPort.openPort();
            serialPort.DataReceived += serialPortDataReceived;
        }
        private static void InitSocket(ConfigKeyBoard config, Priority pri)
        {
            socket = new SocketLib(config, pri);
            socket.OpendSocket();
            socket.DataReceived += ReciveDataSocket;
        }
        private static void InitDic(Dictionary<string, Service> dicSer, Dictionary<string, Counter> dicCount)
        {
            dicService = dicSer;
            dicCounter = dicCount;
        }
        #endregion

        #region LẤY DATA KHI JOIN SOCKET SET CHO DEVICE
        private static void InitKeyBoardSend(EventSocketSendProgram eventData)
        {
            dicService = sortDicServices(eventData.DicService);
            List<string> lstServices = dicService.Values.OrderBy(m => m.NameService).Select(m => m.NameService).ToList();

            if (dicCounter.Count() == 0)
            {
                dicCounter = sortDicCounter(eventData.DicCounter);
                List<string> lstCounters = dicCounter.Values.OrderBy(m => m.CNum).Select(m => m.Name).ToList();


                #region SET SERVICE COUNTER CHO DEVICE
                foreach (var item in config.KeyBoardCounters)
                {
                    int address = item.AddressKeyboard;
                    int numCounter = item.NumCounter;

                    string counterID = dicCounter.Values.FirstOrDefault(m => m.CNum == numCounter).Id;
                    if (!counterID.Equals(""))
                    {
                        item.CounterID = counterID;
                        dicCounterKeyboard.Add(address, item);
                    }

                }
                #endregion
            }
            else
            {
                dicCounter = sortDicCounter(eventData.DicCounter);
            }
            serialPort.SendData(setSrcVcs.SetService(0, lstServices));
            Thread.Sleep(6000);
            var byteSrc = setSrcVcs.SetCounter(0, "Quầy Vé", dicCounter.Values.ToList(), dicCounterKeyboard);
            serialPort.SendData(byteSrc);
            Thread.Sleep(5000);
            var byteReset = modBus.BuildText(DEVICE_KEYBOARD, 0, (int)CheckByteSend.BYTE_COMMAND.RESET_COUNTER, "", -1);
            serialPort.SendData(byteReset);
            Thread.Sleep(1000);
            SendNumToCounter(eventData, false);

            Console.WriteLine("Set services and counter success!");

        }

        #region SEND NUM ĐANG PHỤC VỤ XUỐNG QUẦY
        private static void SendNumToCounter(EventSocketSendProgram eventData, bool isOpened)
        {
            bool isLst = false;
            if (eventData.LstSend != null && eventData.LstSend.Count() > 0)
            {
                isLst = true;
                var command = (int)CheckByteSend.BYTE_COMMAND.NEXT_COMMAND;
                var commandLed = (int)CheckByteSend.COMMAND_LED.DISPLAY_COMMAND;
                foreach (var obj in eventData.LstSend)
                {
                    var addKeyBoard = dicCounterKeyboard.Values.FirstOrDefault(m => m.CounterID == obj.counter_id);
                    if (addKeyBoard != null)
                    {
                        var counterID = addKeyBoard.CounterID;
                        var indexService = getIndexService(obj.service_id, counterID);
                        var data = obj.cnum;
                        var byteRes = modBus.BuildText(DEVICE_KEYBOARD, addKeyBoard.AddressKeyboard, command, data, indexService);
                        serialPort.SendData(byteRes);
                        SendLED(eventData.Action, obj.counter_Old, eventData.DicCounter, commandLed, data);
                    }
                }
            }
            if (isOpened && isLst)
            {
                var lstCounterId = eventData.LstSend.Select(m => m.counter_id);
                var dicOut = dicCounterKeyboard.Where(m => !lstCounterId.Contains(m.Value.CounterID));
                if (dicOut != null && dicOut.Count() > 0)
                {
                    foreach (var item in dicOut)
                    {
                        serialPortError(MessageError.ERROR_09, item.Key, item.Value.CounterID);
                    }
                }
            }

        }
        #endregion

        #endregion

        #region NHẬN DATA SOCKET TRUYỀN CHO DEVICE
        private static void ReciveDataSocket(EventSocketSendProgram eventData)
        {

            int command = 0;
            int commandLed = 0;
            bool isCheckMoveCounter = false;
            string data = null;
            if (eventData.Message == null)
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
                        foreach (var couId in eventData.LstCouter)
                        {
                            var address = dicCounterKeyboard.FirstOrDefault(d => d.Value.CounterID == couId).Key;
                            serialPortError(MessageError.ERROR_00, address, couId);
                        }
                        return;
                    case ActionTicket.ACTION_CALL:
                        command = (int)CheckByteSend.BYTE_COMMAND.NEXT_COMMAND;
                        commandLed = (int)CheckByteSend.COMMAND_LED.DISPLAY_COMMAND;
                        break;
                    case ActionTicket.ACTION_RECALL:
                        command = (int)CheckByteSend.BYTE_COMMAND.RECALL_COMMAND;
                        commandLed = (int)CheckByteSend.COMMAND_LED.DISPLAY_COMMAND;
                        break;
                    case ActionTicket.ACTION_RESTORE:
                        command = (int)CheckByteSend.BYTE_COMMAND.CALLSTORE_COMMAND;
                        commandLed = (int)CheckByteSend.COMMAND_LED.SLIDE_DATA_COMMAND;
                        break;
                    case ActionTicket.ACTION_MOVE:
                        var services = eventData.LstSend[0].extra.services;
                        var counters = eventData.LstSend[0].extra.counters;
                        if (services != null && services.Length > 0)
                        {
                            command = (int)CheckByteSend.BYTE_COMMAND.FORWARD_COMMAND_SERVICE;
                            eventData.LstSend[0].service_id = services[0];
                        }
                        else if (counters != null && counters.Length > 0)
                        {
                            command = (int)CheckByteSend.BYTE_COMMAND.FORWARD_COMMAND_COUNTER;
                            eventData.LstSend[0].counter_id = counters[0];
                            isCheckMoveCounter = true;
                        }
                        commandLed = (int)CheckByteSend.COMMAND_LED.SLIDE_DATA_COMMAND;
                        break;
                    case ActionTicket.ACTION_CANCEL:
                        command = (int)CheckByteSend.BYTE_COMMAND.DELETE_COMMAND;
                        commandLed = (int)CheckByteSend.COMMAND_LED.SLIDE_DATA_COMMAND;
                        break;
                    case ActionTicket.ACTION_FINISH:
                        command = (int)CheckByteSend.BYTE_COMMAND.FINISH_COMMAND;
                        commandLed = (int)CheckByteSend.COMMAND_LED.SLIDE_DATA_COMMAND;
                        break;
                    case ActionTicket.ACTION_ALL_RESTORE:
                        command = (int)CheckByteSend.BYTE_COMMAND.RESTORE_COMMAND;
                        break;
                    case ActionTicket.CALL_LIST_WATTING:
                        command = (int)CheckByteSend.BYTE_COMMAND.CALL_LIST_WATTING;
                        break;
                    case ActionTicket.ACTION_CONNECT_COUNTER:
                        command = (int)CheckByteSend.BYTE_COMMAND.NEXT_COMMAND;
                        break;
                    case ActionTicket.ACTION_PING:
                        commandLed = (int)CheckByteSend.COMMAND_LED.STATUS_COMMAND;
                        break;
                    default:
                        //bug
                        break;
                }
                try
                {
                    if (!eventData.Action.Equals(ActionTicket.ACTION_ALL_RESTORE) &&
                        !eventData.Action.Equals(ActionTicket.CALL_LIST_WATTING) &&
                        eventData.LstSend != null && eventData.LstSend.Count() > 0)
                    {
                        var obj = eventData.LstSend[0];
                        if (eventData.Action.Equals(ActionTicket.ACTION_MOVE) && obj != null && obj.extra != null && obj.extra.counters != null && obj.extra.counters.Count() > 0)
                        {
                            data = obj.cnum;
                            var NumServicesOrCounter = -1;
                            if (isCheckMoveCounter) NumServicesOrCounter = getIndexCounter(obj.counter_id, dicCounter);
                            else NumServicesOrCounter = getIndexService(obj.service_id, obj.counter_Old);
                            var address = dicCounterKeyboard.FirstOrDefault(d => d.Value.CounterID == obj.counter_Old).Key;
                            var byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, command, data, NumServicesOrCounter);
                            serialPort.SendData(byteRes);
                        }
                        else
                        {

                            data = obj.cnum;
                            SendDataToPortHardWear(obj.ticket_id, eventData.CounterId, obj.service_id, data, command, action);

                        }
                        SendLED(eventData.Action, obj.counter_Old, eventData.DicCounter, commandLed, data);
                    }
                    else if (eventData.Action.Equals(ActionTicket.ACTION_ALL_RESTORE) || eventData.Action.Equals(ActionTicket.CALL_LIST_WATTING))
                    {
                        SendDataRetore(eventData.CounterId, eventData.LstSend, command, dicService);
                    }
                    else if (eventData.Action.Equals(ActionTicket.ACTION_PING))
                    {
                        SendLED(eventData.Action, eventData.CounterId, eventData.DicCounter, commandLed, data);
                    }
                }
                catch { }
            }
            else
            {
                var address = dicCounterKeyboard.FirstOrDefault(d => d.Value.CounterID == eventData.CounterId).Key;
                serialPortError(eventData.Message, address, eventData.CounterId);
            }
            resBytes = null;
        }
        #endregion

        #region SERIALPORT NHẬN DATA GỬI LÊN SOCKET
        static void SendDataRetore(string counterID, List<ObjectSend> lstObject, int command, Dictionary<string, Service> dicSer)
        {
            if (lstObject != null && lstObject.Count() > 0)
            {
                var address = dicCounterKeyboard.FirstOrDefault(d => d.Value.CounterID == counterID).Key;
                var byteRes = modBus.BuildTextAllRetore(DEVICE_KEYBOARD, address, command, lstObject, dicSer);
                serialPort.SendData(byteRes);
            }
            else
            {
                Console.WriteLine(MessageError.ERROR_03);
            }
        }

        static void SendDataToPortHardWear(string ticketID, string counterID, string serviceID, string data, int command, string action)
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
                    var byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, command, data, indexService);
                    serialPort.SendData(byteRes);
                }
                else
                {
                    log.sendLog(MessageError.ERROR_03);
                }
            }
            else
            {
                serialPortError(MessageError.ERROR_01, address, counterID);
            }
        }
        static void serialPortError(string data, int address, string counterID)
        {
            byte[] byteRes = null;
            switch (data)
            {
                case MessageError.ERROR_00:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, (int)CheckByteSend.BYTE_COMMAND.ERROR_COMMAND, data, 1);
                    break;
                case MessageError.ERROR_06:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, 0, (int)CheckByteSend.BYTE_COMMAND.ERROR_COMMAND, data, 1);
                    break;
                case MessageError.ERROR_09:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, (int)CheckByteSend.BYTE_COMMAND.ERROR_COMMAND, data, 1);
                    break;
                case MessageError.ERROR_01:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, (int)CheckByteSend.BYTE_COMMAND.ERROR_COMMAND, data, 1);
                    SendLED(ActionTicket.ACTION_FINISH, counterID, null, (int)CheckByteSend.COMMAND_LED.DISPLAY_COMMAND, "STOP");
                    break;
                default:
                    byteRes = modBus.BuildText(DEVICE_KEYBOARD, address, (int)CheckByteSend.BYTE_COMMAND.ERROR_COMMAND, data, 13);
                    break;
            }
            serialPort.SendData(byteRes);
        }
        static void serialPortPing()
        {
            if (serialPort.openPort())
            {
                byte[] byteRes = modBus.BuildText(DEVICE_KEYBOARD, dicCounterKeyboard.Values.ToArray()[0].AddressKeyboard, (int)CheckByteSend.BYTE_COMMAND.STATUS_COMMAND, "", 1);
                serialPort.SendData(byteRes);
            }

        }

        private static void serialPortDataReceived(byte[] data)
        {
            Console.WriteLine("Nhận Data KeyBoard: " + data.Length);
            Console.WriteLine("BYTE RECIVE:");
            foreach (var i in data)
            {
                Console.Write(i + ", ");
            }
            Console.WriteLine("");
            sendQueueByKeyBoard.Enqueue(data);
            if (!comSendWorker.IsBusy)
            {
                comSendWorker.RunWorkerAsync();
            }
        }
        static void comSendWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!comSendWorker.CancellationPending && sendQueueByKeyBoard.Count != 0)
            {
                var data = sendQueueByKeyBoard.Dequeue();
                // nhận data từ bàn phím xử lý gọi lên socket
                resBytes = CheckByteSend.getByteSendList(data, dicCounterKeyboard);
                if (resBytes != null && resBytes.Count() > 0)
                {
                    foreach (var resByte in resBytes)
                    {
                        switch (resByte.DeviceId)
                        {
                            case (int)CheckByteSend.DEVICE_ID.DEVICE_KEYBOARD:
                                string idCounter = "";
                                if (dicCounterKeyboard.ContainsKey(resByte.AddressKey))
                                {
                                    idCounter = dicCounterKeyboard[resByte.AddressKey].CounterID;
                                    var arrAcNum = CheckByteSend.GetActionByCommand(resByte.Command, resByte.NumServicesOrCounter, idCounter, dicService, dicCounter);
                                    string action = arrAcNum[0];
                                    string idCounterOrServiceMove = arrAcNum[1];
                                    Console.WriteLine("action: " + action + ", idCounterOrServiceMove: " + idCounterOrServiceMove);
                                    if (action == null && idCounterOrServiceMove == null)
                                    {
                                        Console.WriteLine("Lỗi Send action: " + MessageError.ERROR_08);
                                        serialPortError(MessageError.ERROR_08, resByte.AddressKey, idCounter);
                                    }
                                    else if (action != null && !"".Equals(action))
                                    {
                                        if (action.Equals(ActionTicket.CALL_PRIORITY) || action.Equals(ActionTicket.ACTION_RESTORE))
                                        {
                                            idCounterOrServiceMove = resByte.Cnum;
                                        }
                                        Console.WriteLine("Send action: " + action + ", idCounterOrServiceMove: " + idCounterOrServiceMove);
                                        socket.SendFromAction(action, idCounter, idCounterOrServiceMove);
                                        Thread.Sleep(100);
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
                                        socket.SendFromAction(ActionTicket.ACTION_RATING, resByteRecive.CounterID, rating + "");
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
        #endregion

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

        #region LED CONTROLLER
        private static void SendPingLED(Dictionary<string, Counter> dicAllCounter, int command)
        {
            var lstCounters = dicCounter.Where(m => m.Value.StateLed == ActionTicket.STATE_SERVING);
            foreach (var item in lstCounters)
            {
                SendDataToLED(item.Value.Id, "", command);
            }
        }

        private static void SendDataToLED(string counterId, string cnum, int command)
        {
            var addr = dicCounterKeyboard.Values.FirstOrDefault(m => m.CounterID == counterId);
            if (addr != null)
            {
                var data = cnum;
                var byteRes = modBus.BuildTextSendLed((int)CheckByteSend.DEVICE_ID.DEVICE_LED, addr.AddressLed, command, data);
                serialPort.SendData(byteRes);
            }
        }

        private static void SendLED(string action, string counterId, Dictionary<string, Counter> dicCounterLed, int command, string data)
        {
            switch (action)
            {
                case ActionTicket.INITIAL:
                    SendDataToLED(counterId, data, command);
                    break;
                case ActionTicket.ACTION_CREATE: // không làm gì
                    SendDataToLED(counterId, data, command);
                    break;
                case ActionTicket.ACTION_CALL:
                    SendDataToLED(counterId, data, command);
                    break;
                case ActionTicket.ACTION_RECALL:

                    SendDataToLED(counterId, data, command);
                    break;
                case ActionTicket.ACTION_RESTORE:
                    break;
                case ActionTicket.ACTION_MOVE:
                    SendDataToLED(counterId, "STOP", command);
                    break;
                case ActionTicket.ACTION_CANCEL:

                    SendDataToLED(counterId, "STOP", command);
                    break;
                case ActionTicket.ACTION_FINISH:
                    SendDataToLED(counterId, "STOP", command);
                    break;
                case ActionTicket.ACTION_PING:
                    SendPingLED(dicCounterLed, command);
                    break;
                default:
                    //bug
                    break;
            }
        }
        #endregion LED CONTROLLER
       
       * */
        #endregion remove
    }
    /*
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
    } */
}
