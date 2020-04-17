using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages
{
    public class Messages
    {
        public string Body { get; set; }
        public string Time { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
    }
}
