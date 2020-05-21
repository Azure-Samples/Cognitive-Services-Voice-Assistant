// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSampleTests
{
    using UWPVoiceAssistantSample;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.ConversationalAgent;
    using Windows.Foundation;
    using Windows.Media.Audio;

    /// <summary>
    /// Mock wrapper for ConversationalAgentSession
    /// </summary>
    public class MockAgentSessionWrapper : IAgentSessionWrapper
    {
        public ConversationalAgentState AgentState { get; set; }

        public bool IsIndicatorLightAvailable { get; set; }

        public bool IsInterrupted { get; set; }

        public bool IsInterruptible { get; set; }

        public bool IsScreenAvailable { get; set; }

        public bool IsUserAuthenticated { get; set; }

        public bool IsVoiceActivationAvailable { get; set; }

        public bool IsSignalVerificationRequired { get; set; }

        public string SignalName { get; set; }
        public TimeSpan SignalStart { get; set; }
        public TimeSpan SignalEnd { get; set; }

        public event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSessionInterruptedEventArgs> SessionInterrupted;
        public event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSignalDetectedEventArgs> SignalDetected;
        public event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSystemStateChangedEventArgs> SystemStateChanged;

        public AudioDeviceInputNode CreateAudioDeviceInputNode(AudioGraph graph)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<AudioDeviceInputNode> CreateAudioDeviceInputNodeAsync(AudioGraph graph)
        {
            throw new NotImplementedException();
        }

        public string GetAudioCaptureDeviceId()
        {
            return string.Empty;
        }

        public IAsyncOperation<string> GetAudioCaptureDeviceIdAsync()
        {
            return Task.FromResult(string.Empty).AsAsyncOperation();
        }

        public object GetAudioClient()
        {
            return new object();
        }

        public IAsyncOperation<object> GetAudioClientAsync()
        {
            return Task.FromResult(new object()).AsAsyncOperation();
        }

        public string GetAudioRenderDeviceId()
        {
            return string.Empty;
        }

        public IAsyncOperation<string> GetAudioRenderDeviceIdAsync()
        {
            return Task.FromResult(string.Empty).AsAsyncOperation();
        }

        public uint GetSignalModelId()
        {
            return 0;
        }

        public IAsyncOperation<uint> GetSignalModelIdAsync()
        {
            return Task.FromResult((uint)0).AsAsyncOperation();
        }

        public IReadOnlyList<uint> GetSupportedSignalModelIds()
        {
            return new List<uint>();
        }

        public IAsyncOperation<IReadOnlyList<uint>> GetSupportedSignalModelIdsAsync()
        {
            return Task.FromResult((IReadOnlyList<uint>)new List<uint>()).AsAsyncOperation();
        }

        public void InitializeHandlers()
        {
        }

        public ConversationalAgentSessionUpdateResponse RequestAgentStateChange(ConversationalAgentState state)
        {
            this.AgentState = state;
            return ConversationalAgentSessionUpdateResponse.Success;
        }

        public IAsyncOperation<ConversationalAgentSessionUpdateResponse> RequestAgentStateChangeAsync(ConversationalAgentState state)
        {
            this.AgentState = state;
            return Task.FromResult(ConversationalAgentSessionUpdateResponse.Success).AsAsyncOperation();
        }

        public ConversationalAgentSessionUpdateResponse RequestForegroundActivation()
        {
            return ConversationalAgentSessionUpdateResponse.Success;
        }

        public IAsyncOperation<ConversationalAgentSessionUpdateResponse> RequestForegroundActivationAsync()
        {
            return Task.FromResult(ConversationalAgentSessionUpdateResponse.Success).AsAsyncOperation();
        }

        public ConversationalAgentSessionUpdateResponse RequestInterruptible(bool interruptible)
        {
            return ConversationalAgentSessionUpdateResponse.Success;
        }

        public IAsyncOperation<ConversationalAgentSessionUpdateResponse> RequestInterruptibleAsync(bool interruptible)
        {
            return Task.FromResult(ConversationalAgentSessionUpdateResponse.Success).AsAsyncOperation();
        }

        public bool SetSignalModelId(uint signalModelId)
        {
            return true;
        }

        public IAsyncOperation<bool> SetSignalModelIdAsync(uint signalModelId)
        {
            return Task.FromResult(true).AsAsyncOperation();
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MockAgentSessionWrapper()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
    }
}
