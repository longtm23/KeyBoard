using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanPhimCung.Command
{
    public class WriteLog
    {
        public void sendLog(string message)
        {
            Console.WriteLine("> log {0}", message);
        }
    }
}
