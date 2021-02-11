// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Static manager class for generating instances of the selected ILogProvider interface. The
    /// type is specified once (via ChosenLoggerType) and then Reflection is used to initialize and
    /// instantiate the appropriate objects.
    /// </summary>
    public static class LogRouter
    {
        private static bool initialized = false;

        /// <summary>
        /// Gets the compile-selected type for the log router to use when creating logger instances.
        /// </summary>
        public static Type ChosenLoggerType { get; } = typeof(NLogProvider);

        /// <summary>
        /// Initializes the necessary global, once-per-app state needed for logging to be properly
        /// configured within the provider. Must be invoked once and only once before using other
        /// methods in LogRouter.
        /// </summary>
        public static void Initialize()
        {
            if (LogRouter.initialized)
            {
                throw new InvalidOperationException();
            }

            LogRouter.ChosenLoggerType.GetMethod("Initialize").Invoke(null, null);
            LogRouter.initialized = true;
        }

        /// <summary>
        /// Gets an instance of the selected ILogProvider implementation configured to prefix its
        /// log statements with the class name of the caller. Uses Reflection to fetch the caller
        /// information and to instantiate the chosen logger (via its type) for use in subsequent
        /// calls.
        /// </summary>
        /// <returns> An instance of the selected ILogProvider implementation. </returns>
        public static ILogProvider GetClassLogger()
        {
            if (!LogRouter.initialized)
            {
                throw new NotSupportedException();
            }

            var callerFrame = new StackFrame(1);
            var methodBase = callerFrame.GetMethod();
            var declaringType = methodBase?.DeclaringType;

            var constructorTypes = new Type[] { typeof(string) };
            var constructorValues = new object[] { declaringType?.Name ?? "Logger" };

            var logger = ChosenLoggerType.GetConstructor(constructorTypes).Invoke(constructorValues);

            return logger as ILogProvider;
        }
    }
}
