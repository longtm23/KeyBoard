using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanPhimCung.Ultility
{
    public class ActionTicket
    {
        public const string TICKET_ONCE = "/ticketsuper?once=";
        public const string RATING_ONCE = "/super/rating?once=";
        public const string ERROR_SERVER = "network_close";
        public const string OPEN_SERVER = "open_server";
        public const string INITIAL = "initial";
        public const string INITIAL_SER = "initial_ser";
        public const string INITIAL_COU = "initial_cou";
        public const string RELOAD = "reload";

        public const string CONNECT = "connect";
        public const string DISCONNECT = "disconnect";

        public const string TICKET_ACTION = "ticket_action";
        public const string MESSAGE_ERROR = "msg_err";

        public const string ACTION_RESET = "reset_all";
        public const string ACTION_PING = "ping";
        public const string ACTION_CALL = "call";
        public const string ACTION_CONNECT_COUNTER = "connect_counter";
        public const string ACTION_RECALL = "recall";
        public const string ACTION_MOVE = "move";
        public const string ACTION_CANCEL = "cancel";
        public const string ACTION_FINISH = "finish";
        public const string ACTION_FEEDBACK = "feedback";
        public const string ACTION_RATING = "rating";
        public const string ACTION_CREATE = "create";
        public const string ACTION_RESTORE = "restore";
        public const string ACTION_ALL_RESTORE = "all_restore";
        public const string ACTION_MOVE_SERVICE = "move_service";
        public const string ACTION_MOVE_COUNTER = "move_counter";
        public const string ACTION_RESET_COUNTER = "reload";
        public const string CALL_LIST_WATTING = "call_list_watting";
        public const string CALL_PRIORITY = "call_priority";
        public const string STATE_WATING = "waiting";
        public const string STATE_CANCELLED = "cancelled";
        public const string STATE_SERVING = "serving";

        public const int LENGH_RANDOM = 5;
        public const string PLATFORM = "keyboard";

        public const string LANG_VI = "vi";
        public const string LANG_SP = "sp";
        public const string LANG_EN = "en";
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;
        public const uint ES_AWAYMODE_REQUIRED = 0x00000040;


        public const string ACTION_SET_SERVICE = "set_ser";
        public const string ACTION_SET_COUNTER = "set_cou";

    }
}
