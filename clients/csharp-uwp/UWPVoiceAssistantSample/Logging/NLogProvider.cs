// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
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
        public static readonly List<string> LogBuffer = new List<string>();
        private Logger logger;

        /// <summary>
        /// Event to indicate a log was generated.
        /// </summary>
        public static event EventHandler logAvailable;

        public event EventHandler LogAvailable
        {
            add
            {
                logAvailable += value;
            }

            remove
            {
                logAvailable -= value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NLogProvider"/> class.
        /// </summary>
        /// <param name="name"> The class name or prefix for the logger to use. </param>
        public NLogProvider(string name)
        {
            this.logger = NLog.LogManager.GetLogger(name);
        }

        /// <summary>
        /// Gets LogBuffer.
        /// </summary>
        List<string> ILogProvider.LogBuffer { get => LogBuffer; }

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
            this.logger.Log(ConvertLogLevel(level), message);
            LogBuffer.Add(message);
            this.OnLogAvailable();
        }

        /// <summary>
        /// Invokes the LogAvailable EventHandler to indicate a log was created.
        /// </summary>
        public void OnLogAvailable()
        {
            logAvailable?.Invoke(this, EventArgs.Empty);
        }

        private static LogLevel ConvertLogLevel(LogMessageLevel level)
        {
            switch (level)
            {
                case LogMessageLevel.Error:
                    return LogLevel.Error;
                case LogMessageLevel.Information:
                    return LogLevel.Info;
                case LogMessageLevel.Noise:
                default:
                    return LogLevel.Trace;
            }
        }
    }
}
