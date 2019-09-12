using BanPhimCung.Controller;
using BanPhimCung.DTO;
using BanPhimCung.Ultility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using WebSocket4Net;

namespace BanPhimCung.Connect_Socket
{
    public sealed class SocketLib
    {
        //public  class SocketLib
        //{
        private WebSocket webSocket = null;

        Dictionary<string, Service> dicAllServices = null;
        Dictionary<string, Counter> dicAllCounters = null;

        private Dictionary<string, Ticket> dicServing = null;
        private Dictionary<string, Ticket> dicWaitting = null;
        private Dictionary<string, Ticket> dicCancelled = null;
        private SocketController socController;

        public static readonly SocketLib Instance = new SocketLib();
        private SocketLib() { }

        public delegate void ReceivedEventHandler(EventSocket objectData);
        public event ReceivedEventHandler DataReceived;
        private Priority resPri;
        private bool isReload = false;
        private ConfigKeyBoard config = null;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SetThreadExecutionState([In] uint esFlags);

        void unParam()
        {
            this.resPri = null;
            initParam();
        }
        private List<Service> GetListService()
        {
            return dicAllServices.Values.ToList();
        }
        public void InitSocketLib(ConfigKeyBoard conf, Priority pri)
        {
            config = conf;
            this.resPri = pri;
            initParam();
            //Init_WebSocket(conf);
            SetTimerPing();
            //InitBackground();
            SetThreadExecutionState(ActionTicket.ES_CONTINUOUS | ActionTicket.ES_SYSTEM_REQUIRED | ActionTicket.ES_AWAYMODE_REQUIRED);
            Init_WebSocket(conf);
        }
        private void Init_WebSocket(ConfigKeyBoard conf)
        {
            string ws = "ws://";
            if (conf.IsHttps)
            {
                ws = "wss://";
            }
            string url = ws + conf.HttpCetm + "/room/actor/join?branch_code=" + conf.StoreCode + "&actor_type=superbox&user_id=" + "&reconnect_count=0&is_uid=1"; ;
            //string url = ws + conf.IsHttps + "/room/actor/join?branch_code=" + conf.StoreCode + "&actor_type=counter&counter_code=" + conf.counter_code + "&user_id=" + conf.user.id + "&reconnect_count=0";
            webSocket = new WebSocket(url);
            webSocket.Opened += websocket_Opened;
            webSocket.Error += websocket_Error;
            webSocket.Closed += websocket_Closed;
            webSocket.MessageReceived += websocket_MessageReceived;
            webSocket.EnableAutoSendPing = true;
            try
            {
                webSocket.Open();
            }
            catch
            {
                OpendSocket();
            }
        }

        public void OpendSocket()
        {
            try
            {
                if (webSocket.State != WebSocketState.Connecting && webSocket.State != WebSocketState.Open)
                {

                    while (webSocket.State != WebSocketState.Open)
                    {
                        webSocket.Open();
                    }

                }
            }
            catch
            {
                SetTimer();
                return;
            }
            isReload = false;
        }
        private System.Timers.Timer aTimer;
        private void SetTimer()
        {
            // Create a timer with a two second interval.
            if (aTimer == null)
            {
                aTimer = new System.Timers.Timer(5000);
                // Hook up the Elapsed event for the timer. 
                aTimer.Elapsed += OnTimedEvent;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;
            }
            else if (!aTimer.Enabled)
            {
                aTimer.Enabled = true;
            }
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (this.webSocket != null)
                {
                    this.webSocket = null;
                }
                KeyBoardCtrl sCtrl = new KeyBoardCtrl();
                if (sCtrl.CheckConfig(config.https, config.StoreCode))
                {
                    Console.WriteLine("================ OnTimedEvent Thanh Cong ===========");
                    Init_WebSocket(config);
                    aTimer.Enabled = false;
                }
            }
            catch
            {
                Console.WriteLine("================ OnTimedEvent ERROR SOCKET ===========");
            }
        }

        private System.Timers.Timer aTimePing;
        private void SetTimerPing()
        {
            // Create a timer with a two second interval.
            if (aTimePing == null)
            {
                aTimePing = new System.Timers.Timer(250000);
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

        private void OnTimedPingEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            EventSocket eSocketRecall = new EventSocket(ActionTicket.ACTION_PING);
            DataReceived(eSocketRecall);
        }
        private void initParam()
        {
            dicCancelled = new Dictionary<string, Ticket>();
            dicWaitting = new Dictionary<string, Ticket>();
            dicServing = new Dictionary<string, Ticket>();
            socController = new SocketController();
            dicAllCounters = new Dictionary<string, Counter>();
            dicAllServices = new Dictionary<string, Service>();
        }
        //private bool isSendFinish = false;
        public void SendSocket(string action, string counterID, List<string> counterMove, List<string> serviceMove, string cNum, int point)
        {
            Ticket tk = null;
            string message = null;
            List<Ticket> lstObject = null;
            switch (action)
            {
                case ActionTicket.ACTION_ALL_RESTORE:
                    var lstService = dicAllCounters[counterID].Services;
                    lstObject = dicCancelled.Values.Where(m => lstService.Contains(m.Service_Id)).OrderByDescending(m => m.PriorityTicket).ThenBy(m => m.MTime).Take(18).ToList();
                    EventSocket eventData = null;
                    if (lstObject == null || lstObject.Count() == 0)
                    {
                        message = MessageError.ERROR_05;
                        eventData = new EventSocket(action, counterID, message);
                    }
                    else
                    {
                        eventData = new EventSocket(action, counterID, lstObject);
                    }

                    DataReceived(eventData);
                    return;
                case ActionTicket.CALL_LIST_WATTING:
                    var counter = dicAllCounters[counterID];
                    lstObject = dicWaitting.Values.Where(m => (m.Counter_Id == counterID) || ((m.Counter_Id == null || m.Counter_Id == "") && counter.Services.Contains(m.Services[0]))).OrderByDescending(m => m.PriorityTicket).ThenBy(m => m.MTime).Take(18).ToList();
                    EventSocket eventListW = null;
                    if (lstObject == null || lstObject.Count() == 0)
                    {
                        eventListW = new EventSocket(action, counterID, MessageError.ERROR_01);
                    }
                    else
                    {
                        eventListW = new EventSocket(action, counterID, lstObject);
                    }
                    DataReceived(eventListW);
                    return;
                case ActionTicket.ACTION_CONNECT_COUNTER:
                    var objSendConnect = dicServing.Values.FirstOrDefault(m => m.Counter_Id == counterID);
                    EventSocket evConnect = new EventSocket(action, objSendConnect, counterID);
                    DataReceived(evConnect);
                    return;
                case ActionTicket.ACTION_CALL:
                    var tkServing = dicServing.Values.FirstOrDefault(m => m.Counter_Id == counterID);
                    tk = socController.GetTicketSendSocket(action, counterID, dicAllCounters, dicWaitting);
                    if (tkServing != null)
                    {
                        if (dicAllCounters.ContainsKey(tkServing.Counter_Id) && tk != null)
                        {
                            var cou = dicAllCounters[tkServing.Counter_Id];
                            cou.isNext = true;

                        }
                        convertObjectAndSend(ActionTicket.ACTION_FINISH, tkServing, serviceMove, counterMove);
                        return;
                    }
                    break;
                case ActionTicket.ACTION_RECALL:
                    tk = dicServing.Values.FirstOrDefault(m => m.Counter_Id == counterID);
                    break;
                case ActionTicket.ACTION_RESTORE:
                    tk = dicCancelled.Values.FirstOrDefault(m => m.CNum == cNum);
                    if (tk != null)
                    {
                        tk.Counter_Id = counterID;
                    }
                    break;
                case ActionTicket.ACTION_CANCEL:
                    Console.WriteLine("COUNTER: " + counterID);
                    tk = dicServing.Values.FirstOrDefault(m => m.Counter_Id == counterID);
                    break;
                case ActionTicket.ACTION_FINISH:
                    tk = dicServing.Values.FirstOrDefault(m => m.Counter_Id == counterID);
                    break;
                case ActionTicket.ACTION_CREATE:
                    break;
                case ActionTicket.CALL_PRIORITY:
                    var tkSer = dicServing.Values.FirstOrDefault(m => m.Counter_Id == counterID);
                    if (tkSer != null)
                    {
                        if (dicAllCounters.ContainsKey(tkSer.Counter_Id))
                        {
                            var cou = dicAllCounters[tkSer.Counter_Id];
                            cou.isNext = true;

                        }
                        convertObjectAndSend(ActionTicket.ACTION_FINISH, tkSer, serviceMove, counterMove);
                    }
                    tk = dicWaitting.Values.FirstOrDefault(m => m.CNum == cNum);
                    if (tk != null)
                    {
                        tk.Counter_Id = counterID;
                        action = ActionTicket.ACTION_CALL;
                    }
                    break;
                case ActionTicket.ACTION_MOVE_SERVICE:
                    tk = dicServing.Values.FirstOrDefault(m => m.Counter_Id == counterID);
                    if (tk != null && GetService(tk).Equals(serviceMove[0]))
                    {
                        sendNotiAction(ActionTicket.ACTION_MOVE_SERVICE, counterID);
                        return;
                    }
                    action = ActionTicket.ACTION_MOVE;
                    break;
                case ActionTicket.ACTION_MOVE_COUNTER:
                    tk = dicServing.Values.FirstOrDefault(m => m.Counter_Id == counterID);
                    if (tk != null)
                    {
                        var cou = (dicAllCounters.ContainsKey(counterMove[0])) ? dicAllCounters[counterMove[0]] : null;
                        if (cou != null)
                        {
                            var isCheckSer = cou.Services.Any(m => m == tk.Services[0]);
                            if (!isCheckSer)
                            {
                                sendNotiAction(ActionTicket.ACTION_MOVE_COUNTER, counterID);
                                return;
                            }
                        }
                    }
                    action = ActionTicket.ACTION_MOVE;
                    break;
                case ActionTicket.RATING_ONCE:
                    tk = dicServing.Values.FirstOrDefault(m => m.Counter_Id == counterID);
                    convertRating(tk.Id, point);
                    return;
                case ActionTicket.ACTION_PING:
                    sendPingSocket();
                    EventSocket eSocketRe = new EventSocket(ActionTicket.ACTION_PING);
                    DataReceived(eSocketRe);
                    return;
                default:
                    break;
            }
            if (tk == null)
            {
                sendNotiAction(action, counterID);
                return;
            }
            convertObjectAndSend(action, tk, serviceMove, counterMove);
        }

        public string GetService(Ticket tk)
        {
            string ser = tk.Service_Id;
            if (string.IsNullOrWhiteSpace(tk.Service_Id))
            {
                ser = tk.Services[0];
            }
            return ser;
        }
        private void sendNotiAction(string action, string counterID)
        {
            string message = "";
            switch (action)
            {
                case ActionTicket.ACTION_ALL_RESTORE:
                    break;
                case ActionTicket.CALL_LIST_WATTING:
                    break;
                case ActionTicket.ACTION_CONNECT_COUNTER:
                    break;
                case ActionTicket.ACTION_CALL:
                    var cou = socController.GetCounterByID(counterID, dicAllCounters);
                    if (cou != null)
                    {
                        cou.isNoTicket = true;
                    }
                    message = MessageError.ERROR_01;
                    break;
                case ActionTicket.ACTION_RECALL:
                    break;
                case ActionTicket.ACTION_RESTORE:
                    message = MessageError.ERROR_11;
                    break;
                case ActionTicket.ACTION_CANCEL:
                    message = MessageError.ERROR_13;
                    break;
                case ActionTicket.ACTION_FINISH:
                    message = MessageError.ERROR_14;
                    break;
                case ActionTicket.ACTION_CREATE:
                    break;
                case ActionTicket.CALL_PRIORITY:
                    message = MessageError.ERROR_10;
                    break;
                case ActionTicket.ACTION_MOVE_SERVICE:
                    message = MessageError.ERROR_16;
                    break;
                case ActionTicket.ACTION_MOVE_COUNTER:
                    message = MessageError.ERROR_08;
                    break;
                case ActionTicket.RATING_ONCE:
                    message = MessageError.ERROR_12;
                    break;
                case ActionTicket.ACTION_PING:
                    return;
                default:
                    break;
            }
            EventSocket ev = new EventSocket(action, counterID, message);
            DataReceived(ev);
        }

        public void SendSocketCanelMoveAll(string action, string ticketID, List<string> lstSer, List<string> lstCou)
        {
            Ticket tk = null;
            switch (action)
            {
                case ActionTicket.ACTION_CANCEL:
                    tk = dicWaitting[ticketID];
                    break;
                case ActionTicket.ACTION_MOVE:
                    tk = dicWaitting[ticketID];
                    break;
                case ActionTicket.ACTION_RESTORE:
                    tk = dicCancelled[ticketID]; break;
                default: break;
            }
            convertObjectAndSend(action, tk, lstSer, lstCou);
        }
        private void convertRating(string ticketID, int point)
        {
            Rating rate = new Rating(ticketID, point);
            string json = MRW_Common.ConvertObjectToJson(rate);
            if (!string.IsNullOrWhiteSpace(json))
            {
                string data = ActionTicket.RATING_ONCE + MRW_Common.RandomString(ActionTicket.LENGH_RANDOM) + " " + json;
                sendDataToSocket(data);
            }
        }
        private void convertObjectAndSend(string action, Ticket tk, List<string> lstSer, List<string> lstCou)
        {
            if (tk != null)
            {
                ObjectSend objSend = null;
                switch (action)
                {
                    case ActionTicket.ACTION_MOVE:
                        objSend = new ObjectSend(action, tk.Id, tk.State, tk.Services[0], tk.Counter_Id, ActionTicket.PLATFORM, true, lstSer, lstCou);
                        break;
                    default:
                        objSend = new ObjectSend(action, tk.Id, tk.Counter_Id, tk.State, tk.Services[0], tk.CNum, tk.MTime, ActionTicket.PLATFORM, true);
                        break;
                }
                string json = ConvertObjectToJson(objSend);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    string data = ActionTicket.TICKET_ONCE + MRW_Common.RandomString(ActionTicket.LENGH_RANDOM) + " " + json;
                    sendDataToSocket(data);
                }
            }
        }

        private void sendDataToSocket(string data)
        {
            if (webSocket == null)
            {
                EventSocket eSocket = new EventSocket(ActionTicket.DISCONNECT);
                DataReceived(eSocket);
                SetTimer();
            }
            else
            {
                this.webSocket.Send(data);
            }
        }
        public Ticket GetTicketCancel(string ticketID)
        {
            Ticket tk = null;
            if (dicCancelled.ContainsKey(ticketID))
            {
                tk = dicCancelled[ticketID];
                tk = pri(tk, tk.Counter_Id);
            }
            return tk;
        }

        public Ticket GetTicketWaiting(string ticketID)
        {
            Ticket tk = null;
            if (dicWaitting.ContainsKey(ticketID))
            {
                tk = dicWaitting[ticketID];
                tk = pri(tk, tk.Counter_Id);
            }
            return tk;
        }

        public Ticket GetTicketServing(string counterID)
        {
            if (dicServing != null && dicServing.Count() > 0)
            {
                return dicServing.Values.FirstOrDefault(m => m.Counter_Id == counterID);
            }
            return null;
        }

        public void SendMove(string ticketID, List<string> lstS, List<string> lstC)
        {
            Ticket tk = null;
            if (dicServing != null && dicServing.ContainsKey(ticketID))
            {
                tk = dicServing[ticketID];
            }
            convertObjectAndSend(ActionTicket.ACTION_MOVE, tk, lstS, lstC);
        }

        public void SendFeedBack(string ticketID, string reasonText)
        {
            Ticket tk = null;
            if (dicServing != null && dicServing.ContainsKey(ticketID))
            {
                tk = dicServing[ticketID];
            }
            if (tk != null)
            {
                var objSend = new ObjectSend(ActionTicket.ACTION_FINISH, tk.Id, tk.State, tk.Services[0], ActionTicket.PLATFORM, false, reasonText);
                string json = ConvertObjectToJson(objSend);
                if (!json.Equals(""))
                {
                    string data = ActionTicket.TICKET_ONCE + MRW_Common.RandomString(ActionTicket.LENGH_RANDOM) + " " + json;
                    sendDataToSocket(data);
                }
            }
        }

        private void SetDataToDic(Dictionary<string, Ticket> dicTickets)
        {
            var dataTicket = dicTickets.Values.ToList();
            foreach (var tk in dataTicket)
            {
                switch (tk.State)
                {
                    case ActionTicket.STATE_CANCELLED:
                        pri(tk, tk.Counter_Id);
                        setAddDic(tk, dicCancelled);
                        break;
                    case ActionTicket.STATE_SERVING:
                        var tkServing = pri(tk, tk.Counter_Id);
                        dicServing[tk.Id] = tkServing;
                        break;
                    case ActionTicket.STATE_WATING:
                        addWaitting(tk);
                        break;
                    default: break;
                }
            }
        }

        private void sendPingSocket()
        {
            string data = "/echo?once=" + MRW_Common.RandomString(ActionTicket.LENGH_RANDOM) + " " + null;
            sendDataToSocket(data);
        }
        private void setAddDic(Ticket tk, Dictionary<string, Ticket> dic)
        {
            if (!dic.ContainsKey(tk.Id))
            {
                dic.Add(tk.Id, tk);
            }
            else
            {
                dic[tk.Id] = tk;
            }
        }
        Ticket pri(Ticket tk, string counterID)
        {
            if (tk != null)
            {
                if (!string.IsNullOrWhiteSpace(counterID))
                {
                    var counter = socController.GetCounterByID(counterID, dicAllCounters);

                    if (counter != null && counter.Pservices != null && counter.Pservices.Any(m => m == tk.Services[0]))
                    {
                        tk.ticket_priority.priority_service = 1;
                    }

                    if (counter.Pservices != null && counter.Pservices.Length > 0)
                    {
                        tk.PriorityService = tk.Services.Any(n => counter.Pservices.Contains(n));
                    }
                }
                tk.PriorityTicket = socController.MathPriority(tk.ticket_priority, resPri);
                if (socController.LogicVip(tk.PriorityTicket, resPri.min_priority_restricted))
                {
                    if (tk.Ticket_Booking != null && !string.IsNullOrWhiteSpace(tk.Ticket_Booking.Id)) tk.IsVipKing = true;
                    else tk.IsVip = true;
                }
                else if (tk.Ticket_Booking != null && !string.IsNullOrWhiteSpace(tk.Ticket_Booking.Id)) tk.IsBooking = true;
                else if (tk.PriorityService) tk.IsPService = true;
                else tk.IsNomarl = true;
            }
            return tk;
        }
        private void addWaitting(Ticket tk)
        {
            tk = pri(tk, tk.Counter_Id);
            if (tk != null)
            {
                dicWaitting.Add(tk.Id, tk);
            }
        }

        private string ConvertObjectToJson(ObjectSend objSend)
        {
            return (objSend == null) ? "" : MRW_Common.ConvertObjectToJson(objSend);
        }

        private string ConvertObjectToJson(Ticket objSend)
        {
            return (objSend == null) ? "" : MRW_Common.ConvertObjectToJson(objSend);
        }

        #region SOCKET PARAM
        public void CloseSocket()
        {
            if (this.webSocket != null && this.webSocket.State != WebSocketState.Closed && this.webSocket.State != WebSocketState.Closing)
            {
                this.webSocket.Close();
                while (true)
                {
                    if (this.webSocket.State == WebSocketState.Closed || this.webSocket.State == WebSocketState.Closing)
                    {
                        return;
                    }
                    Thread.Sleep(200);
                }
            }
        }

        public void CloseSocket(bool isRel)
        {
            isReload = isRel;
            CloseSocket();
            this.webSocket = null;
            unParam();
        }
        public void websocket_Opened(object sender, EventArgs e)
        {
            isDisConect = false;
            EventSocket eSocket = new EventSocket(ActionTicket.CONNECT);
            DataReceived(eSocket);
        }
        public void websocket_Error(object sender, EventArgs e)
        {
            //EventSocket eSocket = new EventSocket(ActionTicket.DISCONNECT, null);
            //DataReceived(eSocket);
        }
        private bool isDisConect = false;
        public void websocket_Closed(object sender, EventArgs e)
        {
            while (this.webSocket != null && this.webSocket.State == WebSocketState.Closed && !isDisConect)
            {
                if (!isReload)
                {
                    isDisConect = true;
                    EventSocket eSocket = new EventSocket(ActionTicket.DISCONNECT);
                    DataReceived(eSocket);
                }

                OpendSocket();
                Thread.Sleep(200);
            }
        }
        public void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Received(e.Message.TrimEnd('\0'));
        }
        #endregion SOCKET PARAM
        private void addDicSrcCounter(List<Service> lstServices, List<Counter> lstCounters, string langCd)
        {
            dicAllServices = socController.setDicServices(lstServices, dicAllServices, langCd);
            dicAllCounters = socController.setDicCounters(lstCounters, dicAllCounters, langCd);
        }
        public void Received(string str)
        {
            var handle = str.Split(' ')[0].Trim('/');
            var data = str.Remove(0, str.IndexOf(' '));
            try
            {
                switch (handle)
                {
                    case ActionTicket.INITIAL:
                        Initial dataUser = JsonConvert.DeserializeObject<Initial>(data);
                        var dicSerNew = socController.setDicServices(dataUser.Services, new Dictionary<string, Service>(), config.LangCd);
                        var dicCouNew = socController.setDicCounters(dataUser.Counters, new Dictionary<string, Counter>(), config.LangCd);
                        var arrCheck = socController.CheckModifyServiceCounter(dicAllCounters, dicAllServices, dicCouNew, dicSerNew);

                        initParam();
                        dicAllCounters = dicCouNew;
                        dicAllServices = dicSerNew;

                        SetDataToDic(dataUser.Tickets);
                        EventSocket eSocket = new EventSocket(ActionTicket.INITIAL, dicAllServices, dicAllCounters, dicServing, arrCheck[0], arrCheck[1]);
                        DataReceived(eSocket);
                        break;
                    case ActionTicket.TICKET_ACTION:
                        var tkAc = JsonConvert.DeserializeObject<TicketAction>(data);
                        addDic(tkAc.Action, tkAc.Ticket, tkAc.Counter_Id, tkAc.Extra);
                        break;
                    case ActionTicket.RELOAD:
                        isReload = true;
                        SetThreadExecutionState(ActionTicket.ES_CONTINUOUS);
                        CloseSocket();
                        Thread.Sleep(100);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loi send data" + ex.Message);
            }
        }

        private void sendNotiTicket(Ticket tk)
        {
            foreach (var cou in dicAllCounters.Values)
            {
                if (cou.isNoTicket && cou.Services.Any(m => m == tk.Services[0]))
                {
                    cou.isNoTicket = false;
                    EventSocket eSocket = new EventSocket(ActionTicket.ACTION_CREATE, cou.Id);
                    DataReceived(eSocket); 
                }

            }

        }
        private void addDic(string action, Ticket tk, string counterIdOld, Extra extra)
        {
            string id = tk.Id;
            bool isLoadServing = false;
            bool isLoadWaitting = false;
            bool isLoadCancel = false;
            switch (action)
            {
                case ActionTicket.ACTION_CREATE:
                    sendNotiTicket(tk);
                    addWaitting(tk);
                    break;
                case ActionTicket.ACTION_CALL:
                    if (dicWaitting.ContainsKey(id))
                    {
                        dicServing[tk.Id] = pri(tk, counterIdOld);
                        dicWaitting.Remove(id);
                        EventSocket eSocketCall = new EventSocket(ActionTicket.ACTION_CALL, counterIdOld, isLoadServing, tk, isLoadWaitting, isLoadCancel);
                        DataReceived(eSocketCall);
                    }
                    break;
                case ActionTicket.ACTION_RECALL:
                    if (dicServing != null && dicServing.ContainsKey(id))
                    {
                        EventSocket eSocketRecall = new EventSocket(ActionTicket.ACTION_RECALL, tk, tk.Counter_Id);
                        DataReceived(eSocketRecall);
                    }
                    break;
                case ActionTicket.ACTION_CANCEL:
                    if (dicServing != null && dicServing.ContainsKey(id))
                    {
                        dicServing.Remove(id);
                        isLoadServing = true;
                        isLoadCancel = true;
                        dicCancelled.Add(id, tk);
                    }

                    if (dicWaitting != null && dicWaitting.ContainsKey(id))
                    {
                        dicWaitting.Remove(id);
                        isLoadWaitting = true;
                    }
                    if (isLoadServing || isLoadWaitting || isLoadCancel)
                    {
                        //tk = pri(tk, counterIdOld);
                        EventSocket eSocketCancel = new EventSocket(ActionTicket.ACTION_CANCEL, counterIdOld, isLoadServing, tk, isLoadWaitting, isLoadCancel);
                        DataReceived(eSocketCancel);
                    }
                    break;
                case ActionTicket.ACTION_MOVE:
                    if (dicServing != null && dicServing.ContainsKey(id))
                    {
                        dicServing.Remove(id);
                        addWaitting(tk);
                        if (extra != null)
                        {
                            if (extra.Counters != null && extra.Counters.Length > 0)
                            {
                                tk.Counter_Id = extra.Counters[0];
                                action = ActionTicket.ACTION_MOVE_COUNTER;
                            }

                            if (extra.Services != null && extra.Services.Length > 0)
                            {
                                tk.Services = extra.Services;
                                action = ActionTicket.ACTION_MOVE_SERVICE;
                            }
                        }
                        EventSocket eSocketMove = new EventSocket(action, tk, counterIdOld);
                        DataReceived(eSocketMove);
                    }
                    break;
                case ActionTicket.ACTION_FINISH:
                    if (dicServing != null && dicServing.ContainsKey(id))
                    {
                        dicServing.Remove(id);

                        if (dicAllCounters.ContainsKey(tk.Counter_Id))
                        {
                            var cou = dicAllCounters[tk.Counter_Id];
                            if (cou.isNext)
                            {
                                var tkCall = socController.GetTicketSendSocket(ActionTicket.ACTION_CALL, cou.Id, dicAllCounters, dicWaitting);
                                cou.isNext = false;
                                if (tkCall != null)
                                {
                                    convertObjectAndSend(ActionTicket.ACTION_CALL, tkCall, null, null);
                                }
                            }
                            else
                            {
                                EventSocket eSocketFinish = new EventSocket(ActionTicket.ACTION_FINISH, tk, tk.Counter_Id);
                                DataReceived(eSocketFinish);
                            }
                        }
                    }
                    break;
                case ActionTicket.ACTION_RESTORE:
                    dicCancelled.Remove(id);
                    addWaitting(tk);
                    EventSocket eSocketRestore = new EventSocket(ActionTicket.ACTION_RESTORE, counterIdOld, isLoadServing, tk, true, true);
                    DataReceived(eSocketRestore);
                    break;
                case ActionTicket.ACTION_FEEDBACK:
                    if (dicServing != null && dicServing.ContainsKey(id))
                    {
                        dicServing[id] = tk;
                    }
                    break;
                default: break;
            }

        }
    }
}
