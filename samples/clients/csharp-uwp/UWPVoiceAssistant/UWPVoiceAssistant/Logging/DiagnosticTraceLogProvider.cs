// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant.Logging
{
    using System.Diagnostics;
    using System.IO;
    using Windows.Storage;

    /// <summary>
    /// A simple implementation of the ILogProvider interface using the built-in .NET Trace class
    /// functionality. Writes to a file in the application data folder.
    /// </summary>
    public class DiagnosticTraceLogProvider : ILogProvider
    {
        private static TextWriter outputFileWriter;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticTraceLogProvider"/> class.
        /// </summary>
        /// <param name="name"> The prefix to associate with messages on this instance. </param>
        public DiagnosticTraceLogProvider(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Once-per-app-lifetime initialization method needed for setup via LogRouter.
        /// </summary>
        public static void Initialize()
        {
            var path = $"{ApplicationData.Current.LocalFolder.Path}\\applicationTraceLog.txt";
            DiagnosticTraceLogProvider.outputFileWriter = File.AppendText(path);
            Trace.Listeners.Add(new TextWriterTraceListener(DiagnosticTraceLogProvider.outputFileWriter));
            Trace.AutoFlush = true;
        }

        /// <summary>
        /// Logs the provided message with a reference to the specified log level.
        /// </summary>
        /// <param name="level"> The log or severity level to associate with the message. </param>
        /// <param name="messageToLog"> The content to be emitted to the log. </param>
        public void Log(LogMessageLevel level, string messageToLog)
        {
            Trace.WriteLine($"{level.ToString()}: {messageToLog}", this.name);
        }

        /// <summary>
        /// Logs the provided message with the standard log level.
        /// </summary>
        /// <param name="messageToLog"> The message to emit to the log. </param>
        public void Log(string messageToLog) =>
            this.Log(LogMessageLevel.Information, messageToLog);
    }
}
