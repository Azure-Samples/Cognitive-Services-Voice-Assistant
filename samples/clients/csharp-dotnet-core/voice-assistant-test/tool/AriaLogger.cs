// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Microsoft.Applications.Events;
    using NAudio.Wave;

    /// <summary>
    /// Manages optional logging to Microsoft Aria.
    /// See this getting started guide: https://www.aria.ms/developers/get-started/onesdk-tutorial-per-platform/dotnet-core-client-start/
    /// </summary>
    public static class AriaLogger
    {
        private static ILogger logger = null;

        private static long EventsRejected = 0;
        private static long EventsRetry = 0;
        private static long EventsDropped = 0;
        private static long EventsSent = 0;
        
        static List<TransmitPolicy> REAL_TIME_FOR_ALL = new List<TransmitPolicy>
        {
            new TransmitPolicy
            {
                ProfileName = "RealTimeForALL",
                Rules = new List<Rules>
                {
                    new Rules
                    {
                        NetCost = NetCost.Unknown, PowerState = PowerState.Unknown,
                        Timers = new Timers { Normal = -1, RealTime = -1 }
                    },
                    new Rules
                    {
                        NetCost = NetCost.Low, PowerState = PowerState.Unknown,
                        Timers = new Timers { Normal = -1, RealTime = 10 }
                    },
                    new Rules
                    {
                        NetCost = NetCost.Low, PowerState = PowerState.Charging,
                        Timers = new Timers { Normal = 10, RealTime = 1 }
                    },
                    new Rules
                    {
                        NetCost = NetCost.Low, PowerState = PowerState.Battery,
                        Timers = new Timers { Normal = 30, RealTime = 10 }
                    },
                }
            }
        };
      
        public static void Log(string type, string dialogID, string dialogDescription)
        {
            if (logger == null)
            {
                return;
            }

            EventProperties props = new EventProperties();
            props.Type = type;
            props.SetProperty("DialogID", dialogID);
            props.SetProperty("DialogDescription", dialogDescription);

            EVTStatus status = logger.LogEvent(props);

            if (status != EVTStatus.OK)
            {
                Trace.TraceError($"Aria LogEvent failed with EVTStatus = {status}");
            }
        }

        public static void Start(string tenantToken)
        {
            if (logger == null)
            {
                logger = LogManager.GetLogger("e3b666a628e547c796df3d4cd72e9515-9a5412e5-dede-4018-ae6b-49a0f40e8fde-7297", out EVTStatus status);

                if (status != EVTStatus.OK)
                {
                    Trace.TraceError($"Aria LogManager.GetLogger failed with EVTStatus = {status}");
                    return;
                }

                LogManager.LoadTransmitProfiles(REAL_TIME_FOR_ALL);
                LogManager.SetTransmitProfile(REAL_TIME_FOR_ALL[0].ProfileName);

                // LogManager.SetNetCost(NetCost.High);
                // LogManager.SetPowerState(PowerState.Battery);

                InitCallbacks();
            }

            LogManager.Start(new LogConfiguration());
            }

        public static void Stop()
        {
            if (logger != null)
            {
                LogManager.UploadNow();
                LogManager.Teardown();
                Trace.TraceInformation($"Aria EventsSent = {EventsSent},  EventsRejected = {EventsRejected}, EventsRetry = {EventsRetry}, EventsDropped = {EventsDropped}");
                logger = null;
            }
        }

        private static void InitCallbacks()
        {
            TelemetryEvents telemetryEvents = LogManager.GetTelemetryEvents(out EVTStatus status);

            if (status != EVTStatus.OK)
            {
                Trace.TraceError($"Aria LogManager.GetTelemetryEvents failed with EVTStatus = {status}");
            }
            else
            {
                EventsRejected = 0;
                EventsRetry = 0;
                EventsDropped = 0;
                EventsSent = 0;

                telemetryEvents.EventsRetrying += (sender, args) =>
                {
                    Trace.TraceInformation($"Aria retry event: {args.RetryReason}, {args.RetryDetails}");
                    Interlocked.Add(ref EventsRetry, args.EventsCount);
                };

                telemetryEvents.EventsDropped += (sender, args) =>
                {
                    Trace.TraceInformation($"Aria event dropped: {args.DroppedReason}, {args.DroppedDetails}");
                    Interlocked.Add(ref EventsDropped, args.EventsCount);
                };

                telemetryEvents.EventsSent += (sender, args) => 
                { 
                    Interlocked.Add(ref EventsSent, args.EventsCount);
                };

                telemetryEvents.EventsRejected += (sender, args) =>
                {
                    Trace.TraceInformation($"Aria event rejected: {args.RejectedReason}, {args.RejectedDetails}");
                    Interlocked.Add(ref EventsRejected, args.EventsCount);
                };
            }
        }
    }
}
