// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using Windows.ApplicationModel.ConversationalAgent;
    using Windows.Foundation;
    using Windows.Media.Audio;

    /// <summary>
    /// Wrapper for ConversationalAgentSession.
    /// </summary>
    public interface IAgentSessionWrapper : IDisposable
    {
        /// <summary>
        /// Occurs when another digital assistant activation signal has been detected.
        /// </summary>
        event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSessionInterruptedEventArgs> SessionInterrupted;

        /// <summary>
        /// Occurs when a Signal for activating a digital assistant is detected.
        /// </summary>
        event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSignalDetectedEventArgs> SignalDetected;

        /// <summary>
        /// Occurs when either the system or the user changes a setting that restricts the
        ///     ability of the digital assistant to perform one or more actions.
        /// </summary>
        event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSystemStateChangedEventArgs> SystemStateChanged;

        /// <summary>
        /// Gets the state of the digital assistant.
        /// </summary>
        ConversationalAgentState AgentState { get; }

        /// <summary>
        /// Gets a value indicating whether the indicator light is available.
        /// </summary>
        bool IsIndicatorLightAvailable { get; }

        /// <summary>
        /// Gets a value indicating whether the ConversationalAgentSession is being interrupted.
        /// </summary>
        bool IsInterrupted { get; }

        /// <summary>
        /// Gets a value indicating whether the ConversationalAgentSession can be interrupted.
        /// </summary>
        bool IsInterruptible { get; }

        /// <summary>
        /// Gets a value indicating whether the screen can be turned on.
        /// </summary>
        bool IsScreenAvailable { get; }

        /// <summary>
        /// Gets a value indicating whether the user is authenticated (for example, the device is locked).
        /// </summary>
        bool IsUserAuthenticated { get; }

        /// <summary>
        /// Gets a value indicating whether the digital assistant can be activated by speech input.
        /// </summary>
        bool IsVoiceActivationAvailable { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the ConversationalAgentSignal needs to be verified.
        /// </summary>
        bool IsSignalVerificationRequired { get; set; }

        /// <summary>
        /// Gets or sets the name of the ConversationalAgentSignal (for example, "Cortana"
        /// or "Alexa").
        /// </summary>
        string SignalName { get; set; }

        /// <summary>
        /// Initializes the session event handlers.
        /// </summary>
        void InitializeHandlers();

        /// <summary>
        /// Asynchronously requests that this ConversationalAgentSession be interruptible
        ///     if the keyword for another digital assistant is detected.
        /// </summary>
        /// <param name="interruptible">True, if interruptible; otherwise, false.</param>
        /// <returns>The result of the asynchronous operation as a ConversationalAgentSessionUpdateResponse.</returns>
        IAsyncOperation<ConversationalAgentSessionUpdateResponse> RequestInterruptibleAsync(bool interruptible);

        /// <summary>
        /// Synchronously requests that this ConversationalAgentSession be interruptible
        ///     if the keyword for another digital assistant is detected.
        /// </summary>
        /// <param name="interruptible">True, if interruptible; otherwise, false.</param>
        /// <returns>A ConversationalAgentSessionUpdateResponse.</returns>
        ConversationalAgentSessionUpdateResponse RequestInterruptible(bool interruptible);

        /// <summary>
        /// Asynchronously requests a state change for the current ConversationalAgentSession.
        /// </summary>
        /// <param name="state">The AgentState requested.</param>
        /// <returns>The result of the asynchronous operation as a ConversationalAgentSessionUpdateResponse.</returns>
        IAsyncOperation<ConversationalAgentSessionUpdateResponse> RequestAgentStateChangeAsync(ConversationalAgentState state);

        /// <summary>
        /// Synchronously requests a state change for the current ConversationalAgentSession.
        /// </summary>
        /// <param name="state">The AgentState requested.</param>
        /// <returns>A ConversationalAgentSessionUpdateResponse.</returns>
        ConversationalAgentSessionUpdateResponse RequestAgentStateChange(ConversationalAgentState state);

        /// <summary>
        /// Asynchronously requests that the digital assistant be activated to the foreground.
        /// </summary>
        /// <returns>The result of the asynchronous operation as a ConversationalAgentSessionUpdateResponse.</returns>
        IAsyncOperation<ConversationalAgentSessionUpdateResponse> RequestForegroundActivationAsync();

        /// <summary>
        /// Synchronously requests that the digital assistant be activated to the foreground.
        /// </summary>
        /// <returns>A ConversationalAgentSessionUpdateResponse.</returns>
        ConversationalAgentSessionUpdateResponse RequestForegroundActivation();

        /// <summary>
        /// Asynchronously retrieves an IAudioClient object that creates and initializes
        ///     an audio stream between your application and the audio rendering device.
        /// </summary>
        /// <returns>The result of the asynchronous operation as an IAudioClient object.</returns>
        IAsyncOperation<object> GetAudioClientAsync();

        /// <summary>
        /// Synchronously retrieves an IAudioClient object that creates and initializes an
        ///     audio stream between your application and the audio rendering device.
        /// </summary>
        /// <returns>The IAudioClient object.</returns>
        object GetAudioClient();

        /// <summary>
        /// Asynchronously creates an audio graph input node.
        /// </summary>
        /// <param name="graph">An audio graph that represents the connected input, output, and submix nodes
        ///     for manipulating and routing audio.</param>
        /// <returns>The result of the asynchronous operation as an AudioDeviceInputNode.</returns>
        IAsyncOperation<AudioDeviceInputNode> CreateAudioDeviceInputNodeAsync(AudioGraph graph);

        /// <summary>
        /// Synchronously creates an audio graph input node.
        /// </summary>
        /// <param name="graph">An audio graph that represents the connected input, output, and submix nodes
        ///     for manipulating and routing audio.</param>
        /// <returns>The AudioDeviceInputNode.</returns>
        AudioDeviceInputNode CreateAudioDeviceInputNode(AudioGraph graph);

        /// <summary>
        /// Asynchronously retrieves the device ID for the current speech input device.
        /// </summary>
        /// <returns>The result of the asynchronous operation as a string.</returns>
        IAsyncOperation<string> GetAudioCaptureDeviceIdAsync();

        /// <summary>
        /// Synchronously retrieves the device ID for the current speech input device.
        /// </summary>
        /// <returns>The ID as a string.</returns>
        string GetAudioCaptureDeviceId();

        /// <summary>
        /// Asynchronously retrieves the device ID for the current speech output device.
        /// </summary>
        /// <returns>The result of the asynchronous operation as a string.</returns>
        IAsyncOperation<string> GetAudioRenderDeviceIdAsync();

        /// <summary>
        /// Synchronously retrieves the device ID for the current speech output device.
        /// </summary>
        /// <returns>The ID as a string.</returns>
        string GetAudioRenderDeviceId();

        /// <summary>
        /// Asynchronously retrieves the unique model identifier of the Signal that activated
        ///     the conversational agent.
        /// </summary>
        /// <returns>When this method completes successfully, it returns a unique model identifier.</returns>
        IAsyncOperation<uint> GetSignalModelIdAsync();

        /// <summary>
        /// Retrieves the unique model identifier of the Signal that activated the conversational agent.
        /// </summary>
        /// <returns>The unique model identifier.</returns>
        uint GetSignalModelId();

        /// <summary>
        /// Asynchronously assigns a unique identifier to the model representing the activation
        ///     audio signal for the conversational agent.
        /// </summary>
        /// <param name="signalModelId">The unique identifier.</param>
        /// <returns>An asynchronous operation with a value of **true** if the model identifier was
        ///     set successfully; otherwise **false**.</returns>
        IAsyncOperation<bool> SetSignalModelIdAsync(uint signalModelId);

        /// <summary>
        /// Assigns a unique identifier to the model representing the activation audio signal
        ///     for the conversational agent.
        /// </summary>
        /// <param name="signalModelId">The unique identifier.</param>
        /// <returns>True, if set successfully. Otherwise, false.</returns>
        bool SetSignalModelId(uint signalModelId);

        /// <summary>
        /// Asynchronously retrieves the collection of unique Signal model identifiers supported
        ///     by the conversational agent.
        /// </summary>
        /// <returns>When this method completes successfully, it returns a collection of unique Signal
        ///     model identifiers.</returns>
        IAsyncOperation<IReadOnlyList<uint>> GetSupportedSignalModelIdsAsync();

        /// <summary>
        /// Retrieves the collection of unique Signal model identifiers supported by the
        ///     conversational agent.
        /// </summary>
        /// <returns>A collection of unique Signal model identifiers.</returns>
        IReadOnlyList<uint> GetSupportedSignalModelIds();
    }
}