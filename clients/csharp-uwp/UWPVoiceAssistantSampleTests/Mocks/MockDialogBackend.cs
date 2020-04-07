// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DialogManagerTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Storage;
    using Windows.UI.WindowManagement;
    using UWPVoiceAssistantSample;

    public class MockDialogBackend
        : IDialogBackend<List<byte>>
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

        public async Task InitializeAsync(StorageFile keywordFile)
        {
        }

        public async Task<string> SendDialogMessageAsync(string message)
        {
            return "mock message id";
        }

        public void SetAudioSource(IDialogAudioInputProvider<List<byte>> source)
        {
        }

        public async Task StartAudioTurnAsync(bool performConfirmation)
        {
        }

        public async Task CancelSignalVerification()
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

        public void SimulateKeywordRecognizing(string keyword)
        {
            Debug.WriteLine($"MockDialogBackend: Simulating KeywordRecognizing event");
            this.KeywordRecognizing?.Invoke(keyword);
        }

        public void SimulateKeywordRecognized(string keyword)
        {
            Debug.WriteLine($"MockDialogBackend: Simulating KeywordRecognized event");
            this.KeywordRecognized?.Invoke(keyword);
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