using Newtonsoft.Json;
using BanPhimCung.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using BanPhimCung.Ultility;

namespace BanPhimCung.Controller
{
    public class KeyBoardCtrl
    {
        public static class SslProtocolsExtensions
        {
            public const SslProtocols Tls12 = (SslProtocols)0x00000C00;
            public const SslProtocols Tls11 = (SslProtocols)0x00000300;
        }
        public static class SecurityProtocolTypeExtensions
        {
            public const SecurityProtocolType Tls12 = (SecurityProtocolType)SslProtocolsExtensions.Tls12;
            public const SecurityProtocolType Tls11 = (SecurityProtocolType)SslProtocolsExtensions.Tls11;
            public const SecurityProtocolType SystemDefault = (SecurityProtocolType)0;
        }
        public bool CheckConfig(string host, string storeCode)
        {
            bool isCheck = false;
            try
            {
                var url = host + "/api/config";
                var jsonSetting = getRequest(host, url);
                var data = JsonConvert.DeserializeObject<Config>(jsonSetting);
                if (data != null)
                {
                    isCheck = true;
                }
            }
            catch
            {
                Console.WriteLine("================ OnTimedEvent ERROR CONTROLL ===========");
                isCheck = false;
            }
            return isCheck;
        }

        public Config GetConfig(string host, string branchCode)
        {
            try
            {
                var url = host + "/api/config?branch_code=" + branchCode;
                var jsonSetting = getRequest(host, url);
                var data = JsonConvert.DeserializeObject<Config>(jsonSetting);
                return data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private static string getRequest(string host, string url)
        {
            HttpClient client = new HttpClient();
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolTypeExtensions.Tls12 | SecurityProtocolTypeExtensions.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };
            client.Timeout = new TimeSpan(0, 0, 3);
            client.BaseAddress = new Uri(host);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(url).Result;
            string res = "";
            if (response.IsSuccessStatusCode)
            {
                res = response.Content.ReadAsStringAsync().Result;
            }
            return res;
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
        public Dictionary<string, string> setDicServices(List<Service> lstService, Dictionary<string, string> dic)
        {

            if (lstService != null)
            {
                foreach (var ser in lstService)
                {
                    if (!dic.ContainsKey(ser.Id))
                    {
                        var vi = ser.l10n.Vi;
                        if (vi == null)
                        {
                            vi = ser.l10n.En;
                        }
                        dic[ser.Id] = vi;
                    }
                }
            }
            return dic;
        }

        public ViewInfoHome ConverTkToView(Ticket tk, Dictionary<string, Service> dicSer, bool isCancel)
        {
            ViewInfoHome view = null;
            if (tk != null)
            {
                string serviceName = "";
                string serID = tk.Services[0];
                if (dicSer.ContainsKey(serID))
                {
                    switch (tk.Lang)
                    {
                        case ActionTicket.LANG_VI: serviceName = dicSer[serID].l10n.Vi; break;
                        case ActionTicket.LANG_SP: serviceName = dicSer[serID].l10n.Sp; break;
                        default:
                            serviceName = dicSer[serID].l10n.En; break;
                    }
                }
                view = new ViewInfoHome(tk.Id, tk.CNum, serviceName, tk.MTime, tk.Customer.Code, tk.PriorityTicket, isCancel, tk.PriorityService, tk.IsVip, tk.IsVipKing, tk.IsNomarl, tk.IsPService, tk.IsBooking);
            }
            return view;
        }

        public List<ViewInfoHome> ConvertDicTkToView(Dictionary<string, Ticket> dicTickets, Dictionary<string, Service> dicSer, bool isCancel)
        {
            List<ViewInfoHome> lstInfoHome = new List<ViewInfoHome>();
            if (dicTickets != null)
            {
                foreach (var tk in dicTickets.Values)
                {
                    var view = ConverTkToView(tk, dicSer, isCancel);
                    lstInfoHome.Add(view);
                }
            }
            return lstInfoHome;
        }


        public List<ViewInfoHome> AddSortListWaitting(Ticket tk, Dictionary<string, Service> dicSer, List<ViewInfoHome> lstViewInfo)
        {
            lstViewInfo.Add(ConverTkToView(tk, dicSer, false));
            return sortWaitting(lstViewInfo);
        }

        private List<ViewInfoHome> sortWaitting(List<ViewInfoHome> lstViewInfo)
        {
            return lstViewInfo.OrderByDescending(n => n.Priority).ThenBy(n => n.MtimeTk).ToList();
        }
        public List<ViewInfoHome> SortListWaitting(List<ViewInfoHome> lstViewInfo, bool isPser)
        {
            //if (isPser)
            //{
            //    List<ViewInfoHome> lstW = new List<ViewInfoHome>();
            //    //var dicGroup = lstViewInfo.GroupBy(m => m.PriorityService).ToDictionary(m => m.Key, m => m.OrderByDescending(n => n.Priority).ThenBy(n => n.MtimeTk).ToList());
            //    //if (dicGroup.ContainsKey(true))
            //    //{
            //    //    lstW.AddRange(dicGroup[true]);
            //    //}
            //    //if (dicGroup.ContainsKey(false))
            //    //{
            //    //    lstW.AddRange(dicGroup[false]);
            //    //}
            //}
            return sortWaitting(lstViewInfo);
        }
    }
}
