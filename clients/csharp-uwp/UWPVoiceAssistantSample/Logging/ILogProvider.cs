// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Abstracted information levels to log, used for individual providers to filter their output
    /// targets based on severity and related rules.
    /// </summary>
    public enum LogMessageLevel
    {
        /// <summary>
        /// ConversationalAgent's session changes - these are related to the changes in Conversation State
        /// </summary>
        ConversationalAgentSignal,

        /// <summary>
        /// Signal Detection events and keyword verification stages.
        /// </summary>
        SignalDetection,

        /// <summary>
        /// Logs relating to audio output.
        /// </summary>
        AudioLogs,

        /// <summary>
        /// Verbose information that's generally diagnostic-only
        /// </summary>
        Noise,

        /// <summary>
        /// Standard log information that's non-exceptional but also not extremely verbose
        /// </summary>
        Information,

        /// <summary>
        /// Error information for exceptional and otherwise unexpected or unhandled conditions
        /// </summary>
        Error,
    }

    /// <summary>
    /// Shared interface abstraction for log providers.
    /// NOTE: in addition to the interface requirements, a log provider must also implement:
    ///     * A static "Initialize" method with no parameters;
    ///     * A constructor with a single string argument for the prefix name to use.
    /// </summary>
    public interface ILogProvider
    {
        /// <summary>
        /// Event to indicate a log generated.
        /// </summary>
        event EventHandler LogAvailable;

        /// <summary>
        /// Gets the list of logs from LogBuffer.
        /// </summary>
        List<string> LogBuffer { get; }

        /// <summary>
        /// Instructs the log provider to emit a log at the specified message level.
        /// </summary>
        /// <param name="level"> The message level at which to emit the log statement. </param>
        /// <param name="messageToLog"> The message to emit via the log provider. </param>
        void Log(LogMessageLevel level, string messageToLog);

        /// <summary>
        /// Instructs the log provider to emit a log at the default message level.
        /// </summary>
        /// <param name="messageToLog"> The message to emit via the log provider. </param>
        void Log(string messageToLog);

        /// <summary>
        /// Instructs the log provider to emit a log at the error message level.
        /// </summary>
        /// <param name="errorMessageToLog"> The message to emit via the log provider. </param>
        void Error(string errorMessageToLog);
    }
}
