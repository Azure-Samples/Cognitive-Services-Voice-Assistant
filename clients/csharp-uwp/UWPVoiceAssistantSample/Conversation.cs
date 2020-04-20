using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWPVoiceAssistantSample
{
    public class Conversation
    {
        public string Body { get; set; }
        //public string Time { get; set; }
        //public bool Sent { get; set; }
        //public bool Received { get; set; }

        public ObservableCollection<Conversation> conversations = new ObservableCollection<Conversation>();
    }
}
