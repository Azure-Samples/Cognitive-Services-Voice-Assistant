// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Class to wrap a single instance of ConversationalAgentSession for an app.
    /// </summary>
    public interface IAgentSessionManager
    {
        /// <summary>
        /// Raised when the state machine for conversational agent state has finished setting
        /// a new state after requests to or responses from the dialog backend have required it.
        /// </summary>
        event EventHandler<DetectionOrigin> SignalDetected;

        /// <summary>
        /// It's not required, but for optimization purposes it's acceptable to cache the ConversationalAgentSession
        /// for the lifetime of the app and use it across background/foreground invocations.
        /// </summary>
        /// <returns>Cached Conversation session state.</returns>
        Task<IAgentSessionWrapper> GetSessionAsync();
    }
}