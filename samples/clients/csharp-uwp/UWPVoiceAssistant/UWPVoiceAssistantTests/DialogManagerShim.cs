using DialogManagerTests;
using UWPVoiceAssistant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;

namespace UWPVoiceAssistantTests
{
    public class DialogManagerShim
        : DialogManager
    {
        protected DialogManagerShim(IDialogBackend backend, DialogAudioOutputAdapter outputAdapter) : base(backend, outputAdapter)
        {

        }

        public static async Task<DialogManager> CreateMockManagerAsync(MockDialogBackend backend)
        {
            var dialogManager = await DialogManager.CreateAsync(backend);
            return dialogManager;
        }
    }
}
