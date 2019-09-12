using BanPhimCung.DTO;
using BanPhimCung.Ultility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BanPhimCung.Controller
{
    public class SocketController
    {
        public Counter GetCounterByList(string counterCode, List<Counter> lstCou)
        {
            return lstCou.FirstOrDefault(m => m.Code == counterCode);
        }

        public Counter GetCounterByID(string counterID, Dictionary<string, Counter> dicCounter)
        {
            if (dicCounter.ContainsKey(counterID))
            {
                return dicCounter[counterID];
            }
            return null;
        }

        public bool[] CheckModifyServiceCounter(Dictionary<string, Counter> dicCouOld, Dictionary<string, Service> dicSerOld, Dictionary<string, Counter> dicCouNew, Dictionary<string, Service> dicSerNew)
        {
            bool isModifyCou = false;
            bool isModifySer = false;
            if (dicSerOld == null || dicCouOld == null)
            {
                isModifyCou = true;
                isModifySer = true;
            }
            else
            {
                if ((dicCouOld.Count() != dicSerNew.Count()) || checkModifyDicCou(dicCouOld, dicCouNew))
                {
                    isModifyCou = true;
                }

                if ((dicSerOld.Count() != dicSerOld.Count()) || checkModifyDicSer(dicSerOld,dicSerNew))
                {
                    isModifySer = true;
                }
            }
            return new bool[2]{isModifyCou,isModifySer};
        }

        private bool checkModifyDicSer(Dictionary<string, Service> dicSerOld, Dictionary<string, Service> dicSerNew)
        {
            bool isCheck= false;
            foreach (var serNew in dicSerNew)
            {
                if (dicSerOld.ContainsKey(serNew.Key))
                {
                    var serOld = dicSerOld[serNew.Key];
                    if (!serOld.NameService.Equals(serNew.Value.NameService))
                    {
                        isCheck = true;
                        break;
                    }
                }
                else
                {
                    isCheck = true;
                    break;
                }
            }
            return isCheck;
        }

        private bool checkModifyDicCou(Dictionary<string, Counter> dicCouOld, Dictionary<string, Counter> dicCouNew)
        {
            bool isCheck = false;
            foreach (var couNew in dicCouNew)
            {
                if (dicCouOld.ContainsKey(couNew.Key))
                {
                    var couOld = dicCouOld[couNew.Key];
                    if (!couOld.Name.Equals(couNew.Value.Name) || couNew.Value.Services != couOld.Services)
                    {
                        isCheck = true;
                        break;
                    }
                }
                else
                {
                    isCheck = true;
                    break;
                }
            }
            return isCheck;
        }

        public Dictionary<string, Service> ConvertListToDic(List<Service> lstSer)
        {
            Dictionary<string, Service> dicSer = new Dictionary<string, Service>();
            foreach (var ser in lstSer)
            {
                dicSer.Add(ser.Id, ser);
            }
            return dicSer;
        }
        public bool LogicVip(int pri, int minPriServer)
        {
            return (pri >= minPriServer) ? true : false;
            
        }
        public int MathPriority(TicketPriority pri, Priority resPri)
        {
            if (pri == null || resPri == null)
            {
                return 0;
            }
            int priority = pri.service_quality;
          
            if (pri.moved_ticket > 0)
            {
                priority += resPri.moved_ticket;
            }
            if (pri.restore_ticket > 0)
            {
                priority += resPri.restore_ticket;
            }
            if (pri.internal_vip_card != null && pri.internal_vip_card.Length > 0)
            {
                priority += resPri.internal_vip_card;
            }
            if (pri.customer_vip_card != null && pri.customer_vip_card.Length > 0)
            {
                priority += resPri.customer_vip_card;
            }
            if (pri.privileged_customer != null && pri.privileged_customer.Length > 0)
            {
                priority += resPri.privileged_customer;
            }
            if (pri.booked_ticket != null && pri.booked_ticket.Length > 0)
            {
                priority += resPri.booked_ticket;
            }
            if (pri.priority_service > 0)
            {
                priority += resPri.priority_service;
            }
            return priority;
        }

        public Dictionary<string, Ticket> sortPriorityTicket(  Dictionary<string, Ticket> dicTkWaitting )
        {
            dicTkWaitting = dicTkWaitting.ToList().OrderBy(m => m.Value.PriorityTicket).ThenBy(m => m.Value.MTime).ToDictionary(m=>m.Key, m=>m.Value);
            return dicTkWaitting;
        }

        public Dictionary<string, ObjectSend> AddDataDicSendToDicRecive(Dictionary<string, ObjectSend> lstSend, Dictionary<string, ObjectSend> lstRecive)
        {
            if (lstSend.Count() > 0 && lstRecive != null)
            {
                foreach (var objSend in lstSend)
                {
                    lstRecive.Add(objSend.Key, objSend.Value);
                }
            }
            else if (lstSend.Count() > 0)
            {
                if (lstRecive == null)
                {
                    lstRecive = new Dictionary<string, ObjectSend>();
                }
                lstRecive = lstSend;
            }
            return lstRecive;
        }



        public ObjectSend GetDataFromDic(string ticketID, Dictionary<string, ObjectSend> dicSend)
        {
            if (ticketID != null && dicSend.ContainsKey(ticketID))
            {
                return dicSend[ticketID];
            }
            return null;
        }
        public Dictionary<string, Service> setDicServices(List<Service> lstService, Dictionary<string, Service> dic, string langCd)
        {

            if (lstService != null && dic!= null)
            {
                foreach (var ser in lstService)
                {
                    if (!dic.ContainsKey(ser.Id))
                    {
                        switch (langCd.ToUpper())
                        {
                            case "VI":
                                ser.NameService = ser.l10n.Vi;
                                break;
                            case "EN":
                                ser.NameService = ser.l10n.En;
                                break;
                            case "ES":
                                ser.NameService = ser.l10n.Es;
                                break;
                            case "SP":
                                ser.NameService = ser.l10n.Sp;
                                break;
                        }
                        if (string.IsNullOrWhiteSpace(ser.NameService))
                        {
                            if (string.IsNullOrWhiteSpace(ser.l10n.Vi))
                            {
                                ser.NameService = ser.l10n.Vi;
                            }else if (string.IsNullOrWhiteSpace(ser.l10n.En))
                            {
                                ser.NameService = ser.l10n.En;
                            }
                            else if (string.IsNullOrWhiteSpace(ser.l10n.Es))
                            {
                                ser.NameService = ser.l10n.Es;
                            }
                            else if (string.IsNullOrWhiteSpace(ser.l10n.Sp))
                            {
                                ser.NameService = ser.l10n.Sp;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(ser.NameService))
                        {
                            ser.NameService = ConvertToUnSign(ser.NameService.ToUpper());
                            dic[ser.Id] = ser;
                        }
                    }
                }
            }
            return dic;
        }
        public  string ConvertToUnSign(string text)
        {
            for (int i = 33; i < 48; i++)
            {
                text = text.Replace(((char)i).ToString(), "");
            }

            for (int i = 58; i < 65; i++)
            {
                text = text.Replace(((char)i).ToString(), "");
            }

            for (int i = 91; i < 97; i++)
            {
                text = text.Replace(((char)i).ToString(), "");
            }
            for (int i = 123; i < 127; i++)
            {
                text = text.Replace(((char)i).ToString(), "");
            }
            text = text.Replace(" ", "-");
            Regex regex = new Regex(@"\p{IsCombiningDiacriticalMarks}+");
            string strFormD = text.Normalize(System.Text.NormalizationForm.FormD);
            return regex.Replace(strFormD, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        }
        public Dictionary<string, Counter> setDicCounters(List<Counter> lstCounter, Dictionary<string, Counter> dic, string langCd)
        {

            if (lstCounter != null)
            {
                foreach (var ser in lstCounter)
                {
                    if (!dic.ContainsKey(ser.Id))
                    {
                        dic[ser.Id] = ser;
                    }
                }
            }
            return dic;
        }

        public Dictionary<string, Ticket> sortDic(IDictionary<string, Ticket> dic)
        {
            var list = dic.ToList().OrderBy(x => x.Value.ticket_priority.moved_ticket).ThenBy(x => x.Value.ticket_priority.restore_ticket).ThenBy(m => m.Value.MTime);
            //list.Sort((pair1, pair2) => pair1.Value.mtime.CompareTo(pair2.Value.mtime));
            return list.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public string CheckCounterExistService(string counterNew, Dictionary<string, Counter> dicCounter, string serviceCounterOld)
        {
            return dicCounter[counterNew].Services.FirstOrDefault(m => m == serviceCounterOld);
        }

        public Ticket GetTicketSendSocket(string action, string counterID, Dictionary<string, Counter> dicCounter, Dictionary<string, Ticket> dicTicket)
        {
            Ticket tk = null;
            if (dicTicket != null && dicTicket.Count() > 0)
            {
                Counter couter = null;
                List<string> lstService = null;
                if (dicCounter.ContainsKey(counterID))
                {
                    couter = dicCounter[counterID];
                    lstService = couter.Services.ToList();
                }

                var lst =  dicTicket.Values.Where( m=> (m.Counter_Id == counterID)
                    || (m.Counter_Id == null && lstService.Contains(m.Services[0])))
                    .OrderByDescending(x => x.PriorityTicket)
                    .ThenBy(x => x.MTime).ToList();
                if (lst != null && lst.Count() > 0)
                {
                    tk = lst.First();
                    if (action.Equals(ActionTicket.ACTION_CALL))
                    {
                        tk.Counter_Id = counterID;
                    }
                }
                else
                {
                    couter.isNoTicket = true;
                }
            }
            return tk;
        }
    }
}
