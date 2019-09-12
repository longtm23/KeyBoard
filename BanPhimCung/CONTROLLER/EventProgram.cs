using BanPhimCung.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BanPhimCung.Controller
{
    public class EventProgram:EventArgs
    {
        private string action;
        private List<string> lstServices;
        private List<string> lstCounter;
        private List<Counter> lstAllCounter;

        public List<Counter> LstAllCounter
        {
            get { return lstAllCounter; }
            set { lstAllCounter = value; }
        }
        private Dictionary<int, KeyBoardCounter> dicAddKey;

        public Dictionary<int, KeyBoardCounter> DicAddKey
        {
            get { return dicAddKey; }
            set { dicAddKey = value; }
        }
        private int address;
        private string cNum;
        private int command;
        private int indexService;
        private string counterID;
        private List<Ticket> lstTicket;
        private Dictionary<string, Service> dicService;
        private int pointRate;

        public int PointRate
        {
          get { return pointRate; }
          set { pointRate = value; }
        }
        public Dictionary<string, Service> DicService
        {
            get { return dicService; }
            set { dicService = value; }
        }
        public List<Ticket> LstTicket
        {
            get { return lstTicket; }
            set { lstTicket = value; }
        }

        public string CounterID
        {
            get { return counterID; }
            set { counterID = value; }
        }
        public int Command
        {
            get { return command; }
            set { command = value; }
        }

        public string CNum
        {
            get { return cNum; }
            set { cNum = value; }
        }
        public int IndexService
        {
            get { return indexService; }
            set { indexService = value; }
        }
        public List<string> LstServices
        {
            get { return lstServices; }
            set { lstServices = value; }
        }
       
        public List<string> LstCounter
        {
            get { return lstCounter; }
            set { lstCounter = value; }
        }

        public int Address
        {
            get { return address; }
            set { address = value; }
        }

        public string Action
        {
            get { return action; }
            set { action = value; }
        }
        public EventProgram(string action)
        {
            this.Action = action;
        }
        public EventProgram(string action, List<string> lstCou, List<string> lstSer)
        {
            this.LstServices = lstSer;
            this.Action = action;
            this.LstCounter = lstCou;
        }
        public EventProgram(string action, int add, string couterID, string cNum, int indexSer)
        {
            this.Action = action;
            this.CounterID = couterID;
            this.CNum = cNum;
            this.IndexService = indexSer;
            this.Address = add;
        }
        public EventProgram(string action, int add, List<string> lstCou)
        {
            this.Action = action;
            this.LstCounter = lstCou;
            this.Address = add;
        }
        public EventProgram(string action, List<Counter> lstCou, Dictionary<int, KeyBoardCounter> dicCou)
        {
            this.Action = action;
            this.LstAllCounter = lstCou;
            this.DicAddKey = dicCou;
        }
        public EventProgram(string action,int address, List<Ticket> lstTicket, Dictionary<string, Service> dicService)
        {
            this.Action = action;
            this.DicService = dicService;
            this.LstTicket = lstTicket;
            this.Address = address;
        }

        public EventProgram(string actionSocket, string couterID,string cNum,  List<string> lstCouterMove,List<string> lstServiceMove)
        {
            this.Action = actionSocket;
            this.LstServices = lstServiceMove;
            this.LstCounter = lstCouterMove;
            
            this.CNum = cNum;
            this.CounterID = couterID;
        }
        public EventProgram(string actionSocket, string couterID, int pointRate)
        {
            this.Action = actionSocket;
            this.CounterID = couterID;
            this.PointRate = pointRate;
        }

    }
}
