// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace VoiceAssistantTest
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Applications.Events;

    /// <summary>
    /// Manages optional logging to Microsoft Aria.
    /// See this getting started guide: https://www.aria.ms/developers/get-started/onesdk-tutorial-per-platform/dotnet-core-client-start/.
    /// </summary>
    public static class AriaLogger
    {
        /// <summary>
        /// Use this as the Aria event name for a successful dialog test.
        /// </summary>
        public const string EventNameDialogSucceeded = "DialogSucceeded";

        /// <summary>
        /// Use this as the Aria event name for a failed dialog test.
        /// </summary>
        public const string EventNameDialogFailed = "DialogFailed";

        private const string EventTypeVoiceAssistantTest = "VoiceAssistantTest";

        private static readonly List<TransmitPolicy> RealTimeForAll = new List<TransmitPolicy>
        {
            new TransmitPolicy
            {
                ProfileName = "RealTimeForALL",
                Rules = new List<Rules>
                {
                    new Rules
                    {
                        NetCost = NetCost.Unknown, PowerState = PowerState.Unknown, Timers = new Timers { Normal = -1, RealTime = -1, },
                    },
                    new Rules
                    {
                        NetCost = NetCost.Low, PowerState = PowerState.Unknown, Timers = new Timers { Normal = -1, RealTime = 10, },
                    },
                    new Rules
                    {
                        NetCost = NetCost.Low, PowerState = PowerState.Charging, Timers = new Timers { Normal = 10, RealTime = 1, },
                    },
                    new Rules
                    {
                        NetCost = NetCost.Low, PowerState = PowerState.Battery, Timers = new Timers { Normal = 30, RealTime = 10, },
                    },
                },
            },
        };

        private static ILogger logger = null;

        private static long eventsRejected = 0;
        private static long eventsRetry = 0;
        private static long eventsDropped = 0;
        private static long eventsSent = 0;

        /// <summary>
        /// Log an event to the Aria cloud.
        /// </summary>
        /// <param name="name">The Aria event name.</param>
        /// <param name="dialogID">The dialog ID.</param>
        /// <param name="dialogDescription">the dialog description.</param>
        public static void Log(string name, string dialogID, string dialogDescription)
        {
            if (logger == null)
            {
                return;
            }

            EventProperties props = new EventProperties
            {
                // Note: for some reason Aria Explorer shows "Type" as lower case value of "Name", and ignores the following line:
                Type = EventTypeVoiceAssistantTest,
                Name = name,
            };

            props.SetProperty("DialogID", dialogID);
            props.SetProperty("DialogDescription", dialogDescription);

            EVTStatus status = logger.LogEvent(props);

            if (status != EVTStatus.OK)
            {
                Trace.TraceError($"Aria LogEvent failed with EVTStatus = {status}");
            }
        }

        /// <summary>
        /// Initialize the Aria logger. Call this once before any calls to Log().
        /// </summary>
        /// <param name="tenantToken">The Aria project key.</param>
        public static void Start(string tenantToken)
        {
            if (logger == null)
            {
                LogManager.Start(new LogConfiguration());

                Trace.TraceInformation($"Aria LogManager.GetLogger for tenantToken {tenantToken}");
                logger = LogManager.GetLogger(tenantToken, out EVTStatus status);

                if (status != EVTStatus.OK)
                {
                    Trace.TraceError($"Aria LogManager.GetLogger failed with EVTStatus = {status}");
                    return;
                }

                InitCallbacks();

                LogManager.LoadTransmitProfiles(RealTimeForAll);
                LogManager.SetTransmitProfile(RealTimeForAll[0].ProfileName);

                // Core is very limited so at the moment we can't detect power and networkCost
                // For that case we exposed this APIs:
                // LogManager.SetNetCost(NetCost.High);
                // LogManager.SetPowerState(PowerState.Battery);
            }
        }

        /// <summary>
        /// Close the Aria logger. Call this once when you are done logging.
        /// </summary>
        public static void Stop()
        {
            if (logger != null)
            {
                LogManager.UploadNow();
                LogManager.Teardown();
                Trace.TraceInformation($"Aria EventsSent = {eventsSent},  EventsRejected = {eventsRejected}, EventsRetry = {eventsRetry}, EventsDropped = {eventsDropped}");
                logger = null;
            }
        }

        /// <summary>
        /// Assign event callbacks, so we can count and report at the end events rejected, dropped, sent and retried.
        /// </summary>
        private static void InitCallbacks()
        {
            TelemetryEvents telemetryEvents = LogManager.GetTelemetryEvents(out EVTStatus status);

            if (status != EVTStatus.OK)
            {
                Trace.TraceError($"Aria LogManager.GetTelemetryEvents failed with EVTStatus = {status}");
            }
            else
            {
                eventsRejected = 0;
                eventsRetry = 0;
                eventsDropped = 0;
                eventsSent = 0;

                telemetryEvents.EventsRetrying += (sender, args) =>
                {
                    Trace.TraceInformation($"Aria retry event: {args.RetryReason}, {args.RetryDetails}");
                    Interlocked.Add(ref eventsRetry, args.EventsCount);
                };

                telemetryEvents.EventsDropped += (sender, args) =>
                {
                    Trace.TraceInformation($"Aria event dropped: {args.DroppedReason}, {args.DroppedDetails}");
                    Interlocked.Add(ref eventsDropped, args.EventsCount);
                };

                telemetryEvents.EventsSent += (sender, args) =>
                {
                    Interlocked.Add(ref eventsSent, args.EventsCount);
                };

                telemetryEvents.EventsRejected += (sender, args) =>
                {
                    Trace.TraceInformation($"Aria event rejected: {args.RejectedReason}, {args.RejectedDetails}");
                    Interlocked.Add(ref eventsRejected, args.EventsCount);
                };
            }
        }
    }
}
