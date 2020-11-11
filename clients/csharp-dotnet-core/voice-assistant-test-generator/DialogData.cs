using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceAssistantTestGenerator
{
    public class DialogData
    {
        public string Description = string.Empty;
        public bool Skip = false;
        public int TurnID = 0;
        public int Sleep = 0;
        public string WavFile = string.Empty;
        public string Utterance = string.Empty;
        public string Activity = string.Empty;
        public bool Keyword = false;
    }
}
