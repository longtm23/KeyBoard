using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanPhimCung.DTO
{
    public class TicketAction
    {
        public string Action { get; set; }
        public Ticket Ticket { get; set; }
        public Extra Extra { get; set; }
        public string Counter_Id { get; set; }
    }
}
public class Extra
{
    public Customer Customer { get; set; }
    public string Kiosk_id { get; set; }
    public string Lang { get; set; }

    public string[] Services { get; set; }
    public string[] Counters { get; set; }
}

public class TicketPriority
{
    public int service_quality { get; set; }
    public string internal_vip_card { get; set; }
    public string customer_vip_card { get; set; }
    public string privileged_customer { get; set; }
    public string booked_ticket { get; set; }
    public int moved_ticket { get; set; }
    public int restore_ticket { get; set; }
    public int priority_service { get; set; }
    public TicketPriority(int restore_ticket, int move_ticket)
    {
        this.moved_ticket = moved_ticket;
        this.restore_ticket = restore_ticket;
    }
}
public class Customer
{
    public string code { get; set; }
    public string service_id { get; set; }
    public string id { get; set; }
    public int mtime { get; set; }
    public string state { get; set; }
    public string branch_id { get; set; }
    public string cnum { get; set; }

}
public class Rating
{
    public string ticket_id { get; set; }
    public int rating { get; set; }
    public Rating(string ticket_id, int rating)
    {
        this.ticket_id = ticket_id;
        this.rating = rating;
    }
}
