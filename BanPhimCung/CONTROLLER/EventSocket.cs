using BanPhimCung.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanPhimCung.Controller
{
    public class EventSocket : System.EventArgs
    {
        private string nameCounter;
        private string action;
        private string message;

        private bool isServing;
        private bool isWaiting;
        private bool isCancel;
        private bool isLoadService;
        private bool isLoadCounter;

        public bool IsLoadCounter
        {
            get { return isLoadCounter; }
            set { isLoadCounter = value; }
        }
        public bool IsLoadService
        {
            get { return isLoadService; }
            set { isLoadService = value; }
        }


        private Dictionary<string, Ticket> dicCancel;
        private Dictionary<string, Ticket> dicWaitting;
        private Dictionary<string, Service> dicService;
        
        private List<Ticket> lstSend;
        private Dictionary<string, Counter> dicCounter;

        private Ticket tkServing;
        private string counterID;

        public EventSocket(string action, Dictionary<string, Service> dicService, Dictionary<string, Counter> lstCounter, Dictionary<string, Ticket> dicServing, bool isLoadSer, bool isLoadCou)
        {
            this.Action = action;
            this.DicCancel = dicCancel;
            this.DicService = dicService;
            this.LstSend = dicServing.Values.ToList();
            this.DicCounter = lstCounter;
            this.IsLoadService = isLoadSer;
            this.IsLoadCounter = isLoadCou;
        }

        public EventSocket(string action,string counterID, List<Ticket> lstSend)
        {
            this.LstSend = lstSend;
            this.Action = action;
            this.CounterID = counterID;
        }
        public EventSocket(string action, string counterID, string message)
        {
            this.CounterID = counterID;
            this.Action = action;
            this.Message = message;
        }
        public EventSocket(string action, string counterID)
        {
            this.CounterID = counterID;
            this.Action = action;
        }
        public EventSocket(string action,string counterID, bool isServing, Ticket tkActivity, bool isWaiting, bool isCancel)
        {
            this.IsServing = isServing;
            this.Action = action;
            this.TkServing = tkActivity;
            this.IsCancel = isCancel;
            this.IsWaiting = isWaiting;
            this.CounterID = counterID;
        }

        public EventSocket(string action, Ticket tkServing, string counterOld)
        {
            this.TkServing = tkServing;
            this.Action = action;
            this.CounterID = counterOld;
        }
        public EventSocket(string action)
        {
            this.Action = action;
        }
        public Ticket TkServing
        {
            get { return tkServing; }
            set { tkServing = value; }
        }
        public string CounterID
        {
            get { return counterID; }
            set { counterID = value; }
        }
        public Dictionary<string, Counter> DicCounter
        {
            get { return dicCounter; }
            set { dicCounter = value; }
        }
        public List<Ticket> LstSend
        {
            get { return lstSend; }
            set { lstSend = value; }
        }
        public string Message
        {
            get { return message; }
            set { message = value; }
        }
        
        public bool IsWaiting
        {
            get { return isWaiting; }
            set { isWaiting = value; }
        }
        

        public bool IsCancel
        {
            get { return isCancel; }
            set { isCancel = value; }
        }
        public bool IsServing
        {
            get { return isServing; }
            set { isServing = value; }
        }
        public string Action
        {
            get { return action; }
            set { action = value; }
        }
        public string NameCounter
        {
            get { return nameCounter; }
            set { nameCounter = value; }
        }
       

        public Dictionary<string, Ticket> DicCancel
        {
            get { return dicCancel; }
            set { dicCancel = value; }
        }
       
        public Dictionary<string, Service> DicService
        {
            get { return dicService; }
            set { dicService = value; }
        }
        public Dictionary<string, Ticket> DicWaitting
        {
            get { return dicWaitting; }
            set { dicWaitting = value; }
        }
        
    }
}
