// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NLog;
    using Windows.Storage;

    /// <summary>
    /// An implementation of the ILogProvider interface that uses the open-source NLog project.
    /// </summary>
    public class NLogProvider : ILogProvider
    {
        /// <summary>
        /// List of log messages.
        /// </summary>
        public static readonly List<string> LogMessageBuffer = new List<string>();
        private static int nextLogIndex = 0;
        private Logger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NLogProvider"/> class.
        /// </summary>
        /// <param name="name"> The class name or prefix for the logger to use. </param>
        public NLogProvider(string name)
        {
            this.logger = NLog.LogManager.GetLogger(name);
        }

        /// <summary>
        /// Raised when a new log entry is available from the provider.
        /// </summary>
        public event EventHandler LogAvailable
        {
            add
            {
                LogAvailableEvent += value;
            }

            remove
            {
                LogAvailableEvent -= value;
            }
        }

        /// <summary>
        /// Event to indicate a log was generated.
        /// </summary>
        private static event EventHandler LogAvailableEvent;

        /// <summary>
        /// Gets LogBuffer.
        /// </summary>
        public List<string> LogBuffer { get => LogMessageBuffer; }

        /// <summary>
        /// Initializes the app-global state needed for NLog to emit to its output locations via
        /// the selected rules. Should be run once and only once before using the logger.
        /// </summary>
        public static void Initialize()
        {
            var path = $"{ApplicationData.Current.LocalFolder.Path}\\applicationLog.txt";
            var configuration = new NLog.Config.LoggingConfiguration();

            // These targets are valid through the entire lifetime of the application and thus
            // the normal IDisposable pattern isn't applicable.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var logToFileTarget = new NLog.Targets.FileTarget("applicationLog")
            {
                FileName = path,
            };
            var logToDebugTarget = new NLog.Targets.DebugTarget("debugOutputLog");
#pragma warning restore CA2000 // Dispose objects before losing scope

            configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, logToFileTarget);
            configuration.AddRule(LogLevel.Info, LogLevel.Fatal, logToDebugTarget);

            NLog.LogManager.Configuration = configuration;
        }

        /// <summary>
        /// Logs an NLog message at the default LogLevel.Info level.
        /// </summary>
        /// <param name="message"> The message to log via NLog. </param>
        public void Log(string message) => this.Log(LogMessageLevel.Information, message);

        /// <summary>
        /// Logs an NLog message at the equivalent LogLevel for the abstracted LogMessageLevel.
        /// </summary>
        /// <param name="level"> The LogMessageLevel to convert and use for the log message. </param>
        /// <param name="message"> The message to log via NLog. </param>
        public void Log(LogMessageLevel level, string message)
        {
            LogMessageBuffer.Add($"{level + message}");

            Task.Run(() =>
            {
                var indexToRead = nextLogIndex;
                nextLogIndex += 1;
                this.logger.Log(ConvertLogLevel(level), LogMessageBuffer[indexToRead]);
                this.OnLogAvailable();
            });
        }

        /// <summary>
        /// Logs an NLog message at the LogLevel.Error level.
        /// </summary>
        /// <param name="message"> The message to log via NLog. </param>
        public void Error(string message) => this.logger.Error(message);

        private static LogLevel ConvertLogLevel(LogMessageLevel level)
        {
            switch (level)
            {
                case LogMessageLevel.Error:
                    return LogLevel.Error;
                case LogMessageLevel.Information:
                    return LogLevel.Info;
                case LogMessageLevel.Noise:
                case LogMessageLevel.ConversationalAgentSignal:
                    return LogLevel.Info;
                case LogMessageLevel.SignalDetection:
                    return LogLevel.Info;
                default:
                    return LogLevel.Trace;
            }
        }

        private void OnLogAvailable()
        {
            LogAvailableEvent?.Invoke(this, EventArgs.Empty);
        }
    }
}
