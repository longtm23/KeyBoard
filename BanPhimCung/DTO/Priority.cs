using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BanPhimCung.DTO
{
    public class Priority
    {
        public int priority_step { get; set; }
        public int internal_vip_card { get; set; }
        public int customer_vip_card { get; set; }
        public int privileged_customer { get; set; }
        public int moved_ticket { get; set; }
        public int booked_ticket { get; set; }
        public int restore_ticket { get; set; }
        public int min_priority_restricted { get; set; }
        public int min_priority_unordered_call { get; set; }
        public int priority_service { get; set; }
    }
    public class Config
    {
        public Priority priority { get; set; }
        public Priority priority_fix { get; set; }
    }
}
