// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Applications.Events;
    using NAudio.Wave;

    /// <summary>
    /// Manages optional logging to Microsoft Aria.
    /// See this getting started guide: https://www.aria.ms/developers/get-started/onesdk-tutorial-per-platform/dotnet-core-client-start/
    /// </summary>
    public static class AriaLogger
    {
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

        public static void Log()
        {
            EVTStatus status = EVTStatus.Fail;
            ILogger mylogger = LogManager.GetLogger("e3b666a628e547c796df3d4cd72e9515-9a5412e5-dede-4018-ae6b-49a0f40e8fde-7297", out status);
            if (status != EVTStatus.OK)
            {
                Trace.TraceError($"Failed getting Aria logger. EVTStatus = {status}");
            }
            else
            {

                InitCallbacks();

                       LogManager.LoadTransmitProfiles(REAL_TIME_FOR_ALL);
                  LogManager.SetTransmitProfile(REAL_TIME_FOR_ALL[0].ProfileName);

                for (int i = 0; i < 10; i++)
                {
                    EventProperties props = new EventProperties();
                    props.Name = "myEvent";
                    props.SetProperty("appLaunched", 1.0, PiiKind.None);
                    status = mylogger.LogEvent(props);
                    if (status != EVTStatus.OK)
                    {
                        Trace.TraceError($"Failed LogEvent. EVTStatus = {status}");
                    }
                }
            }
        }

        public static void Start()
        {
            LogManager.Start(new LogConfiguration());

    

           // LogManager.LoadTransmitProfiles(REAL_TIME_FOR_ALL);
          //  LogManager.SetTransmitProfile(REAL_TIME_FOR_ALL[0].ProfileName);
          // LogManager.SetNetCost(NetCost.High);
          //  LogManager.SetPowerState(PowerState.Battery);
        }

        public static void Stop()
        {
            LogManager.Teardown();
            Trace.TraceInformation($"Aria event count: EventsSent = {EventsSent},  EventsRejected = {EventsRejected}, EventsRetry = {EventsRetry}, EventsDropped = {EventsDropped}");
        }

        ///////////// This is useful in debug mod, it is not recommended in production  \\\\\\\\\\\\\\\\\\\\\\\
        private static long EventsRejected = 0;
        private static long EventsRetry = 0;
        private static long EventsDropped = 0;
        private static long EventsSent = 0;

        public static void InitCallbacks()
        {
            var telemetryEvents = LogManager.GetTelemetryEvents(out EVTStatus value);

            EventsRejected = 0;
            EventsRetry = 0;
            EventsDropped = 0;
            EventsSent = 0;

            telemetryEvents.EventsRetrying += (sender, args) => { Interlocked.Add(ref EventsRetry, args.EventsCount); };
            telemetryEvents.EventsDropped += (sender, args) => { Interlocked.Add(ref EventsDropped, args.EventsCount); };
            telemetryEvents.EventsSent += (sender, args) => { Interlocked.Add(ref EventsSent, args.EventsCount); };
            telemetryEvents.EventsRejected += (sender, args) => { Interlocked.Add(ref EventsRejected, args.EventsCount); };
        }
    }
}
