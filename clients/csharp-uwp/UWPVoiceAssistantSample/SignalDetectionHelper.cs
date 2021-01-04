// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using UWPVoiceAssistantSample.KwsPerformance;
    using Windows.ApplicationModel.ConversationalAgent;

    /// <summary>
    /// Arguments raised when a signal detection has finished being resolved after being surfaced
    /// by the conversational agent activation runtime.
    /// </summary>
    /// <param name="origin"> The entry point the activation signal originated from. </param>
    /// <param name="isVerificationRequired"> Whether the signal requires verification. </param>
    public delegate void SignalReceivedEventArgs(DetectionOrigin origin, bool isVerificationRequired);

    /// <summary>
    /// Arguments raised when a signal detection has finished being resolved after being surfaced
    /// by the conversational agent activation runtime.
    /// </summary>
    /// <param name="origin"> The entry point the activation signal originated from. </param>
    public delegate void SignalResolutionEventArgs(DetectionOrigin origin);

    /// <summary>
    /// Describes the potential sources of an activation signal as received via the
    /// conversational agent activation runtime.
    /// </summary>
    public enum DetectionOrigin
    {
        /// <summary>
        /// Signal origin is from a background task as registered with the operating system.
        /// No intrusive user experience should be implemented until the signal is confirmed
        /// and a rejection of the signal during confirmation should also ensure the process
        /// is exited.
        /// </summary>
        FromBackgroundTask,

        /// <summary>
        /// Signal origin is from the event handler of a Conversational Agent Session and its
        /// relevant event handler. No application lifecycle decisions should be made on the
        /// basis of this signal and it can be assumed that an evaluation of foregrounding
        /// has already happened or is in progress.
        /// </summary>
        FromApplicationObject,

        /// <summary>
        /// Signal origin is from the manual invocation via a microphone icon or similar inside
        /// of the application. No additional in-signal data exists to be verified or otherwise
        /// used; an immediately confirmed listening/processing state should be assumed.
        /// </summary>
        FromPushToTalk,
    }

    /// <summary>
    /// Manages State and facilitates the communication between User Speech and DLS Bot Response.
    /// </summary>
    public class SignalDetectionHelper
    {
        private static readonly TimeSpan MinimumSignalSeparation
            = new TimeSpan(0, 0, 0, 0, 200);

        private readonly object keywordResponseLock;

        private DateTime? lastSignalReceived = null;
        private bool signalNeedsVerification;
        private IAgentSessionManager agentSessionManager;
        private ILogProvider logger;
        private KwsPerformanceLogger kwsPerformanceLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalDetectionHelper"/> class.
        /// </summary>
        /// <param name="agentSessionManager"> The session manager object to use with this helper. </param>
        public SignalDetectionHelper(IAgentSessionManager agentSessionManager)
        {
            this.agentSessionManager = agentSessionManager;
            this.logger = LogRouter.GetClassLogger();
            this.keywordResponseLock = new object();
            this.kwsPerformanceLogger = new KwsPerformanceLogger();
        }

        /// <summary>
        /// Event raised when a signal is confirmed by the conversational agent activation
        /// runtime.
        /// </summary>
        public event SignalReceivedEventArgs SignalReceived;

        /// <summary>
        /// Event raised when a signal is confirmed by the conversational agent activation
        /// runtime.
        /// </summary>
        public event SignalResolutionEventArgs SignalConfirmed;

        /// <summary>
        /// Event raised when a signal is rejected by the conversational agent activation runtime.
        /// </summary>
        public event SignalResolutionEventArgs SignalRejected;

        /// <summary>
        /// Gets the origin associated with the last processed signal detection.
        /// </summary>
        public DetectionOrigin LastDetectedSignalOrigin { get; private set; }

        /// <summary>
        /// Processes a 1st-stage activation signal as received by the conversational agent
        /// activation runtime.
        /// </summary>
        /// <param name="detectionOrigin"> The entry point through which handler received the activation signal (e.g. via background task or in-app event handler). </param>
        public async void HandleSignalDetection(
            DetectionOrigin detectionOrigin = DetectionOrigin.FromBackgroundTask)
        {
            KwsPerformanceLogger.KwsEventFireTime = TimeSpan.FromTicks(DateTime.Now.Ticks);
            var now = DateTime.Now;
            if (this.lastSignalReceived.HasValue && now.Subtract(this.lastSignalReceived.Value).TotalMilliseconds < MinimumSignalSeparation.TotalMilliseconds)
            {
                this.logger.Log(LogMessageLevel.SignalDetection, $"Ignoring signal received so soon after previous!");
                return;
            }

            this.lastSignalReceived = now;
            this.LastDetectedSignalOrigin = detectionOrigin;

            var session = await this.agentSessionManager.GetSessionAsync();
            var signalName = (detectionOrigin == DetectionOrigin.FromPushToTalk)
                ? "Push to talk" : session.SignalName;

            this.logger.Log(LogMessageLevel.SignalDetection, $"Signal ({detectionOrigin}) detected: {signalName}");

            var canSkipVerification =
                detectionOrigin == DetectionOrigin.FromPushToTalk
                || !session.IsSignalVerificationRequired
                || !LocalSettingsHelper.EnableSecondStageKws;
            this.signalNeedsVerification = !canSkipVerification;

            this.SignalReceived?.Invoke(detectionOrigin, this.signalNeedsVerification);

            this.kwsPerformanceLogger.LogSignalReceived(KwsPerformanceLogger.Spotter, "A", "1", KwsPerformanceLogger.KwsEventFireTime.Ticks, KwsPerformanceLogger.KwsStartTime.Ticks, DateTime.Now.Ticks);

            if (!this.signalNeedsVerification)
            {
                this.OnSessionSignalConfirmed(session, detectionOrigin);
            }
        }

        /// <summary>
        /// Takes the next step in keyword verfication or projects the result if the keyword is confirmed or rejected.
        /// </summary>
        /// <param name="recognitionText"> Keyword text, valid unless empty. </param>
        /// <param name="isFinal"> Whether the verification is the final stage or not. </param>
        public async void KeywordRecognitionDuringSignalVerification(string recognitionText, bool isFinal)
        {
            var session = await this.agentSessionManager.GetSessionAsync();
            if (session.AgentState != ConversationalAgentState.Detecting)
            {
                this.logger.Log(LogMessageLevel.SignalDetection, "Abort reaction to keyword, not detecting");
                return;
            }

            if (!isFinal)
            {
                this.logger.Log(LogMessageLevel.SignalDetection, $"KeywordRecognitionDuringSignalVerification: Verifying : {recognitionText}");
            }
            else if (string.IsNullOrEmpty(recognitionText))
            {
                KwsPerformanceLogger.KwsEventFireTime = TimeSpan.FromTicks(DateTime.Now.Ticks);
                this.logger.Log($"KeywordRecognitionDuringSignalVerification: NoMatch");
                this.logger.Log(LogMessageLevel.SignalDetection, $"KeywordRecognitionDuringSignalVerification: NoMatch");
                this.OnSessionSignalRejected(this.LastDetectedSignalOrigin);
            }
            else
            {
                KwsPerformanceLogger.KwsEventFireTime = TimeSpan.FromTicks(DateTime.Now.Ticks);
                this.logger.Log($"KeywordRecognitionDuringSignalVerification: Verified : {recognitionText}");
                this.logger.Log(LogMessageLevel.SignalDetection, $"KeywordRecognitionDuringSignalVerification: Verified : {recognitionText}");
                this.OnSessionSignalConfirmed(session, this.LastDetectedSignalOrigin);
            }
        }

        private void OnSessionSignalConfirmed(IAgentSessionWrapper session, DetectionOrigin origin)
        {
            this.kwsPerformanceLogger.LogSignalReceived("SWKWS", "A", "2", KwsPerformanceLogger.KwsEventFireTime.Ticks, KwsPerformanceLogger.KwsStartTime.Ticks, DateTime.Now.Ticks);

            this.logger.Log(LogMessageLevel.SignalDetection, $"Confirmed signal received, IsUserAuthenticated={session.IsUserAuthenticated.ToString(null)}");
            if (!session.IsUserAuthenticated)
            {
                // This is a launch over the lock screen. It may be prudent to serialize state
                // and relaunch to ensure a fresh and accurate windowing layout.
                // https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.core.coreapplication.requestrestartasync
            }

            this.SignalConfirmed?.Invoke(origin);
        }

        private void OnSessionSignalRejected(DetectionOrigin origin)
        {
            this.kwsPerformanceLogger.LogSignalReceived("SWKWS", "R", "2", KwsPerformanceLogger.KwsEventFireTime.Ticks, KwsPerformanceLogger.KwsStartTime.Ticks, DateTime.Now.Ticks);
            this.logger.Log(LogMessageLevel.SignalDetection, $"Session signal rejected, Signal Origin: {origin}");
            this.SignalRejected?.Invoke(origin);
        }
    }
}
