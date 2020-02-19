using UWPVoiceAssistant;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.WindowManagement;

namespace DialogManagerTests
{
    public class MockDialogBackend
        : IDialogBackend
    {
        public event Action<string> SessionStarted;
        public event Action<string> SessionStopped;
        public event Action<string> KeywordRecognizing;
        public event Action<string> KeywordRecognized;
        public event Action<string> SpeechRecognizing;
        public event Action<string> SpeechRecognized;
        public event Action<DialogResponse> DialogResponseReceived;
        public event Action<DialogErrorInformation> ErrorReceived;
        public object ConfirmationModel => null;

        public async Task InitializeAsync()
        {
        }

        public async Task<string> SendDialogMessageAsync(string message)
        {
            return "mock message id";
        }

        public void SetAudioSource(IDialogAudioInputProvider source)
        {
        }

        public async Task StartAudioTurnAsync(bool performConfirmation)
        {
        }

        public void SimulateSessionStarted(string sessionId)
        {
            Debug.WriteLine($"MockDialogBackend: Simulating SessionStarted event");
            this.SessionStarted?.Invoke(sessionId);
        }

        public void SimulateSpeechRecognizing(string recoText)
        {
            Debug.WriteLine($"MockDialogBackend: Simulating SpeechRecognizing event");
            this.SpeechRecognizing?.Invoke(recoText);
        }

        public void SimulateMessageResponse(DialogResponse response)
        {
            Debug.WriteLine($"MockDialogBackend: Simulating DialogResponseReceived event");
            this.DialogResponseReceived?.Invoke(response);
        }

        public void SimulateSpeechRecognized(string recoText)
        {
            Debug.WriteLine($"MockDialogBackend: Simulating SpeechRecognized event");
            this.SpeechRecognized?.Invoke(recoText);
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}