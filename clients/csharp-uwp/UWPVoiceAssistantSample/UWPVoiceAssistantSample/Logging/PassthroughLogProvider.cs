// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// A simplistic implementation of the ILogProvider interface that only prints its log
    /// statements to console and debug output. Takes no dependencies on external libraries.
    /// </summary>
    public class PassthroughLogProvider : ILogProvider
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="PassthroughLogProvider"/> class.
        /// </summary>
        /// <param name="name"> The class name or prefix to include in log messages. </param>
        public PassthroughLogProvider(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Initialization routine needed for ILogProvider initialization. This implementation
        /// performs no action in response to initialization.
        /// </summary>
        public static void Initialize()
        {
        }

        /// <summary>
        /// Emits a log statement that includes the generic logging level and message that are provided.
        /// </summary>
        /// <param name="level"> The LogMessageLevel to include in the statement. </param>
        /// <param name="messageToLog"> The statement to log. </param>
        public void Log(LogMessageLevel level, string messageToLog)
        {
            var content = $"Console: {this.name} | {level.ToString()}: {messageToLog}";
            Console.WriteLine(content);
            Debug.WriteLine(content);
        }

        /// <summary>
        /// Emits a log statement at the default logging level for the provided message.
        /// </summary>
        /// <param name="messageToLog"> The statement to log. </param>
        public void Log(string messageToLog) => this.Log(LogMessageLevel.Information, messageToLog);
    }
}
