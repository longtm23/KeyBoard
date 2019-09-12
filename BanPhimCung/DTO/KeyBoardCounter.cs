using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BanPhimCung.DTO
{
    public partial class KeyBoardCounter
    {
        public string CounterID { get; set; }
        public int NumCounter { get; set; }
        public int AddressKeyboard { get; set; }
        public int AddressFeedBack { get; set; }
        public int AddressLed { get; set; }
        public string NameCounter { get; set; }
    }

}
