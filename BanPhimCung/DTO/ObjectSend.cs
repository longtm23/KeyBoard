using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanPhimCung.DTO
{
    public class ObjectSend
    {
        public string action { get; set; }
        public string ticket_id { get; set; }
        public string state { get; set; }
        public string cnum { get; set; }
        public int mtime { get; set; }
        public string counter_id { get; set; }
        public string service_id { get; set; }
        public ExtraSend extra { get; set; }
        public ObjectSend(string action, string ticket_id,string counterID, string state, string service_id, string cnum, int mtime, string platform, bool record_transaction)
        {
            this.action = action;
            this.counter_id = counterID;
            this.service_id = service_id;
            this.state = state;
            this.cnum = cnum;
            this.mtime = mtime;
            this.ticket_id = ticket_id;
            ExtraSend extra = new ExtraSend(platform, record_transaction);
            this.extra = extra;
        }
        public ObjectSend(string action, string ticket_id, string state, string service_id,string counterID, string platform, bool record_transaction, List<string> lstSer, List<string> lstCou)
        {
            this.action = action;
            this.service_id = service_id;
            this.state = state;
            this.ticket_id = ticket_id;
            this.counter_id = counterID;
            ExtraSend extra = new ExtraSend(platform, record_transaction, lstSer, lstCou);
            this.extra = extra;
        }

        public ObjectSend(string action, string ticket_id, string state, string service_id, string platform, bool record_transaction, string reasonText)
        {
            this.action = action;
            this.service_id = service_id;
            this.state = state;
            this.ticket_id = ticket_id;
            ExtraSend extra = new ExtraSend(platform, record_transaction, reasonText);
            this.extra = extra;
        }

    }
    public class ExtraSend
    {
        public bool record_transaction { get; set; }
        public string platform { get; set; }
        public List<string> services { get; set; }
        public List<string> counters { get; set; }
        public int rating { get; set; }
        public string reason_text { get; set; }
        public ExtraSend(string platform, bool record_transaction, List<string> sers, List<string> cous)
        {
            this.platform = platform;
            this.record_transaction = record_transaction;
            this.counters = cous;
            this.services = sers;
        }
        public ExtraSend(string platform, bool record_transaction)
        {
            this.platform = platform;
            this.record_transaction = record_transaction;
        }

        public ExtraSend(string platform, bool record_transaction, string reason_text)
        {
            this.platform = platform;
            this.record_transaction = record_transaction;
            this.reason_text = reason_text;
        }
    }


    public class ViewInfoHome
    {
        public string TicketID { get; set; }
        public string Cnum { get; set; }
        public string HangKH { get; set; }
        public string ServiceName { get; set; }
        public int MtimeDelay { get; set; }
        public int MtimeTk { get; set; }
        public int Priority { get; set; }
        public bool PriorityService { get; set; }
        public bool IsDrawRow { get; set; }
        public bool IsVip { get; set; }
        public bool IsVipKing { get; set; }
        public bool IsBooking { get; set; }
        public bool IsNomarl { get; set; }
        public bool IsPService { get; set; }
        public ViewInfoHome(string ticketID, string cNum, string serviceName, int mtimeDelay, string hangKH, int priority, bool isCancel, bool isPriSer, bool isVip, bool isViKing, bool isNormal, bool isPSer, bool isBooking)
        {
            this.PriorityService = isPriSer;
            this.TicketID = ticketID;
            this.Cnum = cNum;
            this.ServiceName = serviceName;
            this.MtimeDelay = mtimeDelay;
            this.HangKH = hangKH;
            this.Priority = priority;
            this.MtimeTk = mtimeDelay;
            this.IsVip = isVip;
            this.IsVipKing = isViKing;
            this.IsNomarl = isNormal;
            this.IsPService = isPriSer;
            this.IsBooking = isBooking;
        }
    
        public ViewInfoHome() { }
    }
}
