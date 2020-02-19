// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CognitiveServices.Speech;
    using Windows.ApplicationModel.ConversationalAgent;

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
        private static readonly TimeSpan SignalConfirmationTimeout
            = new TimeSpan(0, 0, 0, 0, 3000);

        private static readonly TimeSpan MinimumSignalSeparation
            = new TimeSpan(0, 0, 0, 0, 200);

        private static readonly Lazy<SignalDetectionHelper> LazyInstance
            = new Lazy<SignalDetectionHelper>(() =>
            {
                return new SignalDetectionHelper();
            });

        private static Timer secondStageFailsafeTimer;
        private Stopwatch secondStageStopwatch;
        private DateTime? lastSignalReceived = null;
        private bool signalNeedsVerification;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalDetectionHelper"/> class.
        /// </summary>
        private SignalDetectionHelper()
        {
        }

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
        /// Gets the singleton instance of this class and initializes it if it has not
        /// yet been initialized. Will block until completion if needed.
        /// </summary>
        public static SignalDetectionHelper Instance { get => LazyInstance.Value; }

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
            await this.EnsureHandlersAsync();

            var now = DateTime.Now;
            if (this.lastSignalReceived.HasValue && now.Subtract(this.lastSignalReceived.Value).TotalMilliseconds < MinimumSignalSeparation.TotalMilliseconds)
            {
                Debug.WriteLine($"Ignoring signal received so soon after previous!");
                return;
            }

            this.lastSignalReceived = now;
            this.LastDetectedSignalOrigin = detectionOrigin;

            var session = await AppSharedState.GetSessionAsync();
            var signalName = (detectionOrigin == DetectionOrigin.FromPushToTalk)
                ? "Push to talk" : session.Signal?.SignalName;

            Debug.WriteLine($"{Environment.TickCount} : HandleSignalDetection: '{signalName}', {detectionOrigin.ToString()}");

            var canSkipVerification =
                detectionOrigin == DetectionOrigin.FromPushToTalk
                || !session.Signal.IsSignalVerificationRequired
                || !LocalSettingsHelper.EnableSecondStageKws;
            this.signalNeedsVerification = !canSkipVerification;

            var dialogManager = await DialogManager.GetInstanceAsync();
            await dialogManager.StartConversationAsync(
                detectionOrigin,
                this.signalNeedsVerification);

            if (this.signalNeedsVerification)
            {
                this.StartFailsafeTimer();
            }
            else
            {
                this.OnSessionSignalConfirmed(session, detectionOrigin);
            }
        }

        private async Task EnsureHandlersAsync()
        {
            var dialogManager = await DialogManager.GetInstanceAsync();
            dialogManager.KeywordRecognizing += (sender, text)
                => this.KeywordRecognitionDuringSignalVerification(text, isFinal: false);
            dialogManager.KeywordRecognized += (sender, text)
                => this.KeywordRecognitionDuringSignalVerification(text, isFinal: true);
            dialogManager.DialogStateChanged += this.DialogStateChangeDuringSignalVerification;
        }

        private void DialogStateChangeDuringSignalVerification(ConversationalAgentState oldState, ConversationalAgentState newState)
        {
            if (this.signalNeedsVerification
                && oldState == ConversationalAgentState.Detecting
                && newState == ConversationalAgentState.Inactive)
            {
                this.OnSessionSignalRejected(this.LastDetectedSignalOrigin);
            }
        }

        private async void KeywordRecognitionDuringSignalVerification(string recognitionText, bool isFinal)
        {
            var session = await AppSharedState.GetSessionAsync();
            if (session.AgentState != ConversationalAgentState.Detecting)
            {
                return;
            }

            this.StopFailsafeTimer();
            var dialogManager = await DialogManager.GetInstanceAsync();

            if (!isFinal)
            {
                Debug.WriteLine($"KeywordRecognitionDuringSignalVerification: Verifying : {recognitionText}");
            }
            else if (string.IsNullOrEmpty(recognitionText))
            {
                Debug.WriteLine($"KeywordRecognitionDuringSignalVerification: NoMatch");
                await dialogManager.ChangeAgentStateAsync(ConversationalAgentState.Inactive);
                this.OnSessionSignalRejected(this.LastDetectedSignalOrigin);
            }
            else
            {
                Debug.WriteLine($"KeywordRecognitionDuringSignalVerification: Verified : {recognitionText}");
                await dialogManager.ChangeAgentStateAsync(ConversationalAgentState.Listening);
                this.OnSessionSignalConfirmed(session, this.LastDetectedSignalOrigin);
            }
        }

        private void OnSessionSignalConfirmed(ConversationalAgentSession session, DetectionOrigin origin)
        {
            this.StopFailsafeTimer();

            Debug.WriteLine(message: $"Confirmed signal received, IsUserAuthenticated={session.IsUserAuthenticated.ToString(null)}");
            if (!session.IsUserAuthenticated)
            {
                // This is a launch over the lock screen. It may be prudent to serialize state
                // and relaunch to ensure a fresh and accurate windowing layout.
                // https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.core.coreapplication.requestrestartasync
            }

            this.SignalConfirmed?.Invoke(origin);
        }

        private async void OnSessionSignalRejected(DetectionOrigin origin)
        {
            this.StopFailsafeTimer();
            var dialogManager = await DialogManager.GetInstanceAsync();
            await dialogManager.StopAudioCaptureAsync();
            this.SignalRejected?.Invoke(origin);
        }

        private void StartFailsafeTimer()
        {
            this.secondStageStopwatch = Stopwatch.StartNew();
            secondStageFailsafeTimer = new Timer(
                async _ =>
                {
                    var session = await AppSharedState.GetSessionAsync();
                    if (session.AgentState == ConversationalAgentState.Detecting)
                    {
                        Debug.WriteLine($"Failsafe timer expired; rejecting");
                        var dialogManager = await DialogManager.GetInstanceAsync();
                        await dialogManager.FinishConversationAsync();
                    }
                },
                null,
                (int)SignalConfirmationTimeout.TotalMilliseconds,
                Timeout.Infinite);
        }

        private void StopFailsafeTimer()
        {
            this.secondStageStopwatch?.Stop();
            if (secondStageFailsafeTimer == null)
            {
                return;
            }

            Debug.WriteLine($"{Environment.TickCount} : Cancelling 2nd-stage failsafe timer. Elapsed: {this.secondStageStopwatch?.ElapsedMilliseconds}ms");
            secondStageFailsafeTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            secondStageFailsafeTimer?.Dispose();
            secondStageFailsafeTimer = null;
        }
    }
}
