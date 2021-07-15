// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using Windows.ApplicationModel.ConversationalAgent;
    using Windows.Foundation;
    using Windows.Media.Audio;

    /// <summary>
    /// Wraps ConversationalAgentSession.
    /// </summary>
    public class AgentSessionWrapper : IAgentSessionWrapper
    {
        private readonly ConversationalAgentSession session;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentSessionWrapper"/> class.
        /// </summary>
        /// <param name="session">Session this instance wraps.</param>
        public AgentSessionWrapper(ConversationalAgentSession session)
        {
            this.session = session;
            Contract.Assert(session != null);
        }

        /// <summary>
        /// Occurs when another digital assistant activation signal has been detected.
        /// </summary>
        public event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSessionInterruptedEventArgs> SessionInterrupted;

        /// <summary>
        /// Occurs when a Signal for activating a digital assistant is detected.
        /// </summary>
        public event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSignalDetectedEventArgs> SignalDetected;

        /// <summary>
        /// Occurs when either the system or the user changes a setting that restricts the
        ///     ability of the digital assistant to perform one or more actions.
        /// </summary>
        public event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSystemStateChangedEventArgs> SystemStateChanged;

        /// <summary>
        /// Gets the state of the digital assistant.
        /// </summary>
        public ConversationalAgentState AgentState
        {
            get { return this.session.AgentState; }
        }

        /// <summary>
        /// Gets a value indicating whether the indicator light is available.
        /// </summary>
        public bool IsIndicatorLightAvailable
        {
            get { return this.session.IsIndicatorLightAvailable; }
        }

        /// <summary>
        /// Gets a value indicating whether the ConversationalAgentSession is being interrupted.
        /// </summary>
        public bool IsInterrupted
        {
            get { return this.session.IsInterrupted; }
        }

        /// <summary>
        /// Gets a value indicating whether the ConversationalAgentSession can be interrupted.
        /// </summary>
        public bool IsInterruptible
        {
            get { return this.session.IsInterruptible; }
        }

        /// <summary>
        /// Gets a value indicating whether the screen can be turned on.
        /// </summary>
        public bool IsScreenAvailable
        {
            get { return this.session.IsScreenAvailable; }
        }

        /// <summary>
        /// Gets a value indicating whether the user is authenticated (for example, the device is locked).
        /// </summary>
        public bool IsUserAuthenticated
        {
            get { return this.session.IsUserAuthenticated; }
        }

        /// <summary>
        /// Gets a value indicating whether the digital assistant can be activated by speech input.
        /// </summary>
        public bool IsVoiceActivationAvailable
        {
            get { return this.session.IsVoiceActivationAvailable; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ConversationalAgentSignal needs to be verified.
        /// </summary>
        public bool IsSignalVerificationRequired
        {
            get { return this.session.Signal.IsSignalVerificationRequired; }
            set { this.session.Signal.IsSignalVerificationRequired = value; }
        }

        /// <summary>
        /// Gets or sets the name of the ConversationalAgentSignal (for example, "Cortana").
        /// </summary>
        public string SignalName
        {
            get { return this.session.Signal.SignalName; }
            set { this.session.Signal.SignalName = value; }
        }

        /// <summary>
        /// Gets or sets the SignalStart Time from ConversationalAgentSignal.
        /// </summary>
        public TimeSpan SignalStart
        {
            get { return this.session.Signal.SignalStart; }
            set { this.session.Signal.SignalStart = value; }
        }

        /// <summary>
        /// Gets or sets the SignalEnd Time from ConversationalAgentSignal.
        /// </summary>
        public TimeSpan SignalEnd
        {
            get { return this.session.Signal.SignalEnd; }
            set { this.session.Signal.SignalEnd = value; }
        }

        /// <summary>
        /// Initializes the session event handlers.
        /// </summary>
        public void InitializeHandlers()
        {
            this.session.SessionInterrupted += (ConversationalAgentSession sender, ConversationalAgentSessionInterruptedEventArgs args) =>
            {
                this.SessionInterrupted.Invoke(sender, args);
            };
            this.session.SignalDetected += (ConversationalAgentSession sender, ConversationalAgentSignalDetectedEventArgs args) =>
            {
                this.SignalDetected.Invoke(sender, args);
            };
            this.session.SystemStateChanged += (ConversationalAgentSession sender, ConversationalAgentSystemStateChangedEventArgs args) =>
            {
                this.SystemStateChanged.Invoke(sender, args);
            };
        }

        /// <summary>
        /// Asynchronously requests that this ConversationalAgentSession be interruptible
        ///     if the keyword for another digital assistant is detected.
        /// </summary>
        /// <param name="interruptible">True, if interruptible; otherwise, false.</param>
        /// <returns>The result of the asynchronous operation as a ConversationalAgentSessionUpdateResponse.</returns>
        public IAsyncOperation<ConversationalAgentSessionUpdateResponse> RequestInterruptibleAsync(bool interruptible)
        {
            return this.session.RequestInterruptibleAsync(interruptible);
        }

        /// <summary>
        /// Synchronously requests that this ConversationalAgentSession be interruptible
        ///     if the keyword for another digital assistant is detected.
        /// </summary>
        /// <param name="interruptible">True, if interruptible; otherwise, false.</param>
        /// <returns>A ConversationalAgentSessionUpdateResponse.</returns>
        public ConversationalAgentSessionUpdateResponse RequestInterruptible(bool interruptible)
        {
            return this.session.RequestInterruptible(interruptible);
        }

        /// <summary>
        /// Asynchronously requests a state change for the current ConversationalAgentSession.
        /// </summary>
        /// <param name="state">The AgentState requested.</param>
        /// <returns>The result of the asynchronous operation as a ConversationalAgentSessionUpdateResponse.</returns>
        public IAsyncOperation<ConversationalAgentSessionUpdateResponse> RequestAgentStateChangeAsync(ConversationalAgentState state)
        {
            return this.session.RequestAgentStateChangeAsync(state);
        }

        /// <summary>
        /// Synchronously requests a state change for the current ConversationalAgentSession.
        /// </summary>
        /// <param name="state">The AgentState requested.</param>
        /// <returns>A ConversationalAgentSessionUpdateResponse.</returns>
        public ConversationalAgentSessionUpdateResponse RequestAgentStateChange(ConversationalAgentState state)
        {
            return this.session.RequestAgentStateChange(state);
        }

        /// <summary>
        /// Asynchronously requests that the digital assistant be activated to the foreground.
        /// </summary>
        /// <returns>The result of the asynchronous operation as a ConversationalAgentSessionUpdateResponse.</returns>
        public IAsyncOperation<ConversationalAgentSessionUpdateResponse> RequestForegroundActivationAsync()
        {
            return this.session.RequestForegroundActivationAsync();
        }

        /// <summary>
        /// Synchronously requests that the digital assistant be activated to the foreground.
        /// </summary>
        /// <returns>A ConversationalAgentSessionUpdateResponse.</returns>
        public ConversationalAgentSessionUpdateResponse RequestForegroundActivation()
        {
            return this.session.RequestForegroundActivation();
        }

        /// <summary>
        /// Asynchronously retrieves an IAudioClient object that creates and initializes
        ///     an audio stream between your application and the audio rendering device.
        /// </summary>
        /// <returns>The result of the asynchronous operation as an IAudioClient object.</returns>
        public IAsyncOperation<object> GetAudioClientAsync()
        {
            return this.session.GetAudioClientAsync();
        }

        /// <summary>
        /// Synchronously retrieves an IAudioClient object that creates and initializes an
        ///     audio stream between your application and the audio rendering device.
        /// </summary>
        /// <returns>The IAudioClient object.</returns>
        public object GetAudioClient()
        {
            return this.session.GetAudioClient();
        }

        /// <summary>
        /// Asynchronously creates an audio graph input node.
        /// </summary>
        /// <param name="graph">An audio graph that represents the connected input, output, and submix nodes
        ///     for manipulating and routing audio.</param>
        /// <returns>The result of the asynchronous operation as an AudioDeviceInputNode.</returns>
        public IAsyncOperation<AudioDeviceInputNode> CreateAudioDeviceInputNodeAsync(AudioGraph graph)
        {
            return this.session.CreateAudioDeviceInputNodeAsync(graph);
        }

        /// <summary>
        /// Synchronously creates an audio graph input node.
        /// </summary>
        /// <param name="graph">An audio graph that represents the connected input, output, and submix nodes
        ///     for manipulating and routing audio.</param>
        /// <returns>The AudioDeviceInputNode.</returns>
        public AudioDeviceInputNode CreateAudioDeviceInputNode(AudioGraph graph)
        {
            return this.session.CreateAudioDeviceInputNode(graph);
        }

        /// <summary>
        /// Asynchronously retrieves the device ID for the current speech input device.
        /// </summary>
        /// <returns>The result of the asynchronous operation as a string.</returns>
        public IAsyncOperation<string> GetAudioCaptureDeviceIdAsync()
        {
            return this.session.GetAudioCaptureDeviceIdAsync();
        }

        /// <summary>
        /// Synchronously retrieves the device ID for the current speech input device.
        /// </summary>
        /// <returns>The ID as a string.</returns>
        public string GetAudioCaptureDeviceId()
        {
            return this.session.GetAudioCaptureDeviceId();
        }

        /// <summary>
        /// Asynchronously retrieves the device ID for the current speech output device.
        /// </summary>
        /// <returns>The result of the asynchronous operation as a string.</returns>
        public IAsyncOperation<string> GetAudioRenderDeviceIdAsync()
        {
            return this.session.GetAudioRenderDeviceIdAsync();
        }

        /// <summary>
        /// Synchronously retrieves the device ID for the current speech output device.
        /// </summary>
        /// <returns>The ID as a string.</returns>
        public string GetAudioRenderDeviceId()
        {
            return this.session.GetAudioRenderDeviceId();
        }

        /// <summary>
        /// Asynchronously retrieves the unique model identifier of the Signal that activated
        ///     the conversational agent.
        /// </summary>
        /// <returns>When this method completes successfully, it returns a unique model identifier.</returns>
        public IAsyncOperation<uint> GetSignalModelIdAsync()
        {
            return this.session.GetSignalModelIdAsync();
        }

        /// <summary>
        /// Retrieves the unique model identifier of the Signal that activated the conversational agent.
        /// </summary>
        /// <returns>The unique model identifier.</returns>
        public uint GetSignalModelId()
        {
            return this.session.GetSignalModelId();
        }

        /// <summary>
        /// Asynchronously assigns a unique identifier to the model representing the activation
        ///     audio signal for the conversational agent.
        /// </summary>
        /// <param name="signalModelId">The unique identifier.</param>
        /// <returns>An asynchronous operation with a value of **true** if the model identifier was
        ///     set successfully; otherwise **false**.</returns>
        public IAsyncOperation<bool> SetSignalModelIdAsync(uint signalModelId)
        {
            return this.session.SetSignalModelIdAsync(signalModelId);
        }

        /// <summary>
        /// Assigns a unique identifier to the model representing the activation audio signal
        ///     for the conversational agent.
        /// </summary>
        /// <param name="signalModelId">The unique identifier.</param>
        /// <returns>True, if set successfully. Otherwise, false.</returns>
        public bool SetSignalModelId(uint signalModelId)
        {
            return this.session.SetSignalModelId(signalModelId);
        }

        /// <summary>
        /// Asynchronously retrieves the collection of unique Signal model identifiers supported
        ///     by the conversational agent.
        /// </summary>
        /// <returns>When this method completes successfully, it returns a collection of unique Signal
        ///     model identifiers.</returns>
        public IAsyncOperation<IReadOnlyList<uint>> GetSupportedSignalModelIdsAsync()
        {
            return this.session.GetSupportedSignalModelIdsAsync();
        }

        /// <summary>
        /// Retrieves the collection of unique Signal model identifiers supported by the
        ///     conversational agent.
        /// </summary>
        /// <returns>A collection of unique Signal model identifiers.</returns>
        public IReadOnlyList<uint> GetSupportedSignalModelIds()
        {
            return this.session.GetSupportedSignalModelIds();
        }

        /// <summary>
        /// This code added to correctly implement the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Free disposable resources per the IDisposable interface.
        /// </summary>
        /// <param name="disposing"> Whether managed state is being disposed. </param>
        protected virtual void Dispose(bool disposing)
        {
            this.session.Dispose();
        }
    }
}
