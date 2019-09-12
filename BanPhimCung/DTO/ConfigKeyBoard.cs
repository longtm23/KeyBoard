using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BanPhimCung.DTO
{
    public class ConfigKeyBoard
    {
        public bool IsHttps { get; set; }
        public string https { get; set; }
        public string HttpCetm { get; set; }
        public string Com { get; set; }
        public bool Hide { get; set; }
        public string LangCd { get; set; }
        public string StoreCode { get; set; }
        public List<KeyBoardCounter> KeyBoardCounters { get; set; }
    }
}
