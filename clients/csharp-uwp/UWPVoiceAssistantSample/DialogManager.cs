// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;
    using UWPVoiceAssistantSample.KwsPerformance;
    using Windows.ApplicationModel.ConversationalAgent;

    /// <summary>
    /// Event arguments raised when a dialog changes state.
    /// </summary>
    /// <param name="previousState"> The state being left during this change. </param>
    /// <param name="nextState"> The state being entered during this change. </param>
    public delegate void DialogStateChangeEventArgs(
        ConversationalAgentState previousState,
        ConversationalAgentState nextState);

    /// <summary>
    /// Base abstract class. Contains the common methods in both MVA only and MVA+DLS only scenarios.
    /// </summary>
    /// <typeparam name="TInputType">Input type of audio.</typeparam>
    public class DialogManager<TInputType> : IDialogManager, IDisposable
    {
        private ILogProvider logger;
        private KwsPerformanceLogger kwsPerformanceLogger;
        private IDialogBackend<TInputType> dialogBackend;
        private IDialogAudioInputProvider<TInputType> dialogAudioInput;
        private IDialogAudioOutputAdapter dialogAudioOutput;
        private SignalDetectionHelper signalDetectionHelper;
        private IKeywordRegistration keywordRegistration;
        private IAgentSessionManager agentSessionManager;
        private DialogResponseQueue dialogResponseQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogManager{TInputType}"/> class.
        /// </summary>
        /// <param name="dialogBackend"> The dialog backend for the manager to use. </param>
        /// <param name="keywordRegistration"> The keyword registration with the current keyword file information.</param>
        /// <param name="dialogAudioInput"> The input audio provider. </param>
        /// <param name="agentSessionManager"> The manager that provides the instance of agent session wrapper. </param>
        /// <param name="dialogAudioOutput"> The dialog audio output sink to use. </param>
        public DialogManager(
            IDialogBackend<TInputType> dialogBackend,
            IKeywordRegistration keywordRegistration,
            IDialogAudioInputProvider<TInputType> dialogAudioInput,
            IAgentSessionManager agentSessionManager,
            IDialogAudioOutputAdapter dialogAudioOutput = null)
        {
            Contract.Requires(dialogBackend != null);
            Contract.Requires(agentSessionManager != null);
            this.logger = LogRouter.GetClassLogger();
            this.kwsPerformanceLogger = new KwsPerformanceLogger();
            this.dialogBackend = dialogBackend;
            this.dialogBackend.SessionStarted += (id)
                => this.logger.Log(LogMessageLevel.ConversationalAgentSignal, $"DialogManager: Session start: {id}");
            this.dialogBackend.SessionStopped += (id)
                => this.logger.Log(LogMessageLevel.ConversationalAgentSignal, $"DialogManager: Session stop: {id}");
            this.dialogBackend.KeywordRecognizing += this.OnKeywordRecognizing;
            this.dialogBackend.KeywordRecognized += this.OnKeywordRecognized;
            this.dialogBackend.SpeechRecognizing += this.OnSpeechRecognizing;
            this.dialogBackend.SpeechRecognized += async (text)
                => await this.OnSpeechRecognizedAsync(text);
            this.dialogBackend.DialogResponseReceived += this.OnActivityReceived;
            this.dialogBackend.ErrorReceived += async (errorInformation)
                => await this.OnErrorReceivedAsync(errorInformation);
            this.dialogAudioInput = dialogAudioInput;
            this.keywordRegistration = keywordRegistration;
            this.agentSessionManager = agentSessionManager;

            this.agentSessionManager.SignalDetected += (sender, args) => this.HandleSignalDetection(args);
            this.InitializeSignalDetectionHelper();

            _ = this.InitializeAsync(dialogAudioOutput);
        }

        /// <summary>
        /// Raised when the state machine for conversational agent state has finished setting
        /// a new state after requests to or responses from the dialog backend have required it.
        /// </summary>
        public event DialogStateChangeEventArgs DialogStateChanged;

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
        /// Raised when the backend produces an intermediate "hypothesis" for the ongoing speech-
        /// to-text it's currently performing. These results can change as the user continues
        /// speaking and the use of the text should be limited to low-error-impact utilization like
        /// a responsive visualization of what's being spoken.
        /// </summary>
        public event EventHandler<string> SpeechRecognizing;

        /// <summary>
        /// Raised when the backend produces its final speech-to-text result for a user utterance.
        /// This is the same text provided to the assistant implementation for business logic
        /// processing and can be considered as necessary for further client-side actions.
        /// </summary>
        public event EventHandler<string> SpeechRecognized;

        /// <summary>
        /// Raised when the backend produces an assistant-implementation-specific message for
        /// the client to consume and act on. The format of these responses may vary, but
        /// typically appear as JSON payloads with schemas defined by the backend being used.
        /// </summary>
        public event EventHandler<DialogResponse> DialogResponseReceived;

        /// <summary>
        /// Gets or sets a value indicating whether the current conversation is requesting that it
        /// be continued after the latest turn completes.
        /// </summary>
        protected bool ConversationContinuationRequested { get; set; } = false;

        /// <summary>
        /// Gets the concurrency protection object used when making turn-based dialog
        /// operations.
        /// </summary>
        protected SemaphoreSlim DialogTurnSemaphore { get; } =
            new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets a value indicating whether this object has already processed a Dispose() call.
        /// </summary>
        protected bool AlreadyDisposed { get; private set; } = false;

        /// <summary>
        /// Completes async initialization for dialog manager, including initialization of dialog output.
        /// </summary>
        /// <param name="dialogAudioOutput"> The dialog audio output sink to use. </param>
        /// <returns> A task that completes once the state is updated and consumers are notified. </returns>
        public async Task InitializeAsync(IDialogAudioOutputAdapter dialogAudioOutput = null)
        {
            this.dialogAudioOutput = dialogAudioOutput ?? await DialogAudioOutputAdapter.CreateAsync();
            this.dialogResponseQueue = new DialogResponseQueue(this.dialogAudioOutput);

            this.dialogResponseQueue.ExecutingResponse += async (DialogResponse response) =>
            {
                if (response.MessageMedia != null)
                {
                    await this.ChangeAgentStateAsync(ConversationalAgentState.Speaking);
                }
            };

            this.dialogResponseQueue.ResponseExecuted += async (DialogResponse response) =>
            {
                await this.FinishTurnAsync();
            };

            if (this.dialogAudioOutput != null)
            {
                this.dialogAudioOutput.OutputEnded += async () =>
                {
                    await this.StopAudioPlaybackAsync();
                    var session = await this.agentSessionManager.GetSessionAsync();
                    if (session.AgentState == ConversationalAgentState.Speaking)
                    {
                        await this.ChangeAgentStateAsync(ConversationalAgentState.Inactive);
                    }
                };
            }
        }

        /// <summary>
        /// Processes a 1st-stage activation signal as received by the conversational agent
        /// activation runtime.
        /// </summary>
        /// <param name="detectionOrigin"> The entry point through which handler received the activation signal (e.g. via background task or in-app event handler). </param>
        public void HandleSignalDetection(DetectionOrigin detectionOrigin = DetectionOrigin.FromBackgroundTask)
        {
            this.signalDetectionHelper.HandleSignalDetection(detectionOrigin);
        }

        /// <summary>
        /// Method invoked at the end of conversation, when no subsequent interaction is needed.
        /// </summary>
        /// <returns> A task that completes when the conversation is fully finished. </returns>
        public virtual async Task FinishConversationAsync()
        {
            await this.ChangeAgentStateAsync(ConversationalAgentState.Inactive);
        }

        /// <summary>
        /// Sends an activity as defined by a derived class.
        /// </summary>
        /// <param name="activityJson"> The activity, encoded as json. </param>
        /// <returns> A task that completes once the activity is sent. </returns>
        public async Task<string> SendActivityAsync(string activityJson)
        {
            await this.dialogBackend.InitializeAsync(await this.keywordRegistration.GetConfirmationKeywordFileAsync());
            await this.StopAudioCaptureAsync();
            await this.StopAudioPlaybackAsync();

            var id = await this.dialogBackend.SendDialogMessageAsync(activityJson);
            return id;
        }

        /// <summary>
        /// Stops the flow of audio from the input audio graph and, if created and available,
        /// refreshes the header information on a debug audio capture file to facilitate easy
        /// use of the file in media player applications.
        /// </summary>
        /// <returns> A task that completes once audio capture has been stopped. </returns>
        public async Task StopAudioCaptureAsync()
        {
            await this.dialogAudioInput?.StopAsync();
        }

        /// <summary>
        /// Stops the playback of audio on the current dialog backend audio playback provider,
        /// if available.
        /// </summary>
        /// <returns> A task that completes once playback is stopped. </returns>
        public async Task StopAudioPlaybackAsync()
        {
            await this.dialogAudioOutput?.StopPlaybackAsync();
            await this.dialogResponseQueue.AbortAsync();
        }

        /// <summary>
        /// This code added to correctly implement the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Entry point to begin a conversation (one or multiple turns) for a dialog manager.
        /// </summary>
        /// <param name="signalOrigin"> The entry point for the activation signal. </param>
        /// <param name="signalVerificationRequired">
        ///     Whether or not this conversation's signal should be validated before taking user
        ///     facing action.
        /// </param>
        /// <returns> A task that completes once the conversation is started. </returns>
        public virtual async Task StartConversationAsync(
            DetectionOrigin signalOrigin,
            bool signalVerificationRequired)
        {
            var setupSuccessful = await this.SetupConversationAsync(signalOrigin);
            if (!setupSuccessful)
            {
                this.logger.Log(LogMessageLevel.ConversationalAgentSignal, $"DialogManager2::SetupConversationAsync didn't succeed in setting up a conversation (see earlier logs). Aborting conversation.");
                return;
            }

            await this.StartTurnAsync(signalVerificationRequired);
        }

        /// <summary>
        /// Common logic executed at the beginning of a single- or multi-turn conversation prior
        /// to any turns.
        /// </summary>
        /// <param name="signalOrigin"> The entry point for the activation signal. </param>
        /// <returns> A task that completes once the common logic is finished. </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Desired for robust flow")]
        protected virtual async Task<bool> SetupConversationAsync(DetectionOrigin signalOrigin)
        {
            var session = await this.agentSessionManager.GetSessionAsync();
            try
            {
                if (signalOrigin == DetectionOrigin.FromPushToTalk)
                {
                    await this.dialogAudioInput.InitializeFromNowAsync();
                }
                else
                {
                    await this.dialogAudioInput.InitializeFromAgentSessionAsync(session);
                }
            }
            catch (Exception ex)
            {
                this.logger.Log(LogMessageLevel.Error, $"Unable to acquire MVA 1st-pass audio. Rejecting signal.\n{ex.HResult}: {ex.Message}");
                await this.FinishConversationAsync();
                return false;
            }

            this.dialogBackend.SetAudioSource(this.dialogAudioInput);
            await this.dialogBackend.InitializeAsync(await this.keywordRegistration.GetConfirmationKeywordFileAsync());

            return true;
        }

        /// <summary>
        /// Common behavior to all dialog implementations prior to beginning data transmission
        /// to implementation-specific endpoints.
        /// </summary>
        /// <param name="signalVerificationRequired">
        ///     Whether additional keyword verification should be performed by the configured
        ///     dialog backend prior to user-perceived action.
        /// </param>
        /// <returns> A task to be completed when all common steps are finished. </returns>
        protected virtual async Task StartTurnAsync(bool signalVerificationRequired)
        {
            this.logger.Log(LogMessageLevel.Noise, "Starting a turn");

            var newState = signalVerificationRequired
                ? ConversationalAgentState.Detecting
                : ConversationalAgentState.Listening;
            await this.ChangeAgentStateAsync(newState);

            KwsPerformanceLogger.KwsStartTime = TimeSpan.FromTicks(DateTime.Now.Ticks);

            await this.dialogBackend.StartAudioTurnAsync(signalVerificationRequired);

            var audioToSkip = signalVerificationRequired
                ? TimeSpan.Zero
                : AgentAudioInputProvider.InitialKeywordTrimDuration;
            this.dialogAudioInput.DebugAudioCaptureFilesEnabled = LocalSettingsHelper.EnableAudioCaptureFiles;
            await this.dialogAudioInput.StartWithInitialSkipAsync(audioToSkip);
        }

        /// <summary>
        /// Common logic executed at the end of a single turn of interaction.
        /// </summary>
        /// <returns> A task that completes when the common logic is finished. </returns>
        protected virtual async Task FinishTurnAsync()
        {
            if (this.ConversationContinuationRequested)
            {
                this.ConversationContinuationRequested = false;
                this.dialogBackend.SetAudioSource(this.dialogAudioInput);
                await this.dialogAudioInput.InitializeFromNowAsync();
                await this.StartTurnAsync(signalVerificationRequired: false);
            }
            else
            {
                await this.FinishConversationAsync();
            }
        }

        /// <summary>
        /// Free disposable resources per the IDisposable interface.
        /// </summary>
        /// <param name="disposing"> Whether managed state is being disposed. </param>
        protected virtual async void Dispose(bool disposing)
        {
            if (!this.AlreadyDisposed)
            {
                if (disposing)
                {
                    await this.StopAudioCaptureAsync();
                    await this.StopAudioPlaybackAsync();
                    this.dialogBackend?.Dispose();
                    this.dialogAudioInput?.Dispose();
                    this.dialogAudioOutput?.Dispose();
                }

                this.dialogBackend = null;
                this.dialogAudioInput = null;
                this.dialogAudioOutput = null;
                this.AlreadyDisposed = true;
            }
        }

        /// <summary>
        /// Instructs the manager to update the current ConversationalAgentSessionState to the
        /// requested value and inform consumers of the change.
        /// Indicates ConversationalSessionState changes based on requests and responses to and from the Bot.
        /// </summary>
        /// <param name="newState"> The new, requested conversational agent state. </param>
        /// <returns> A task that completes once the state is updated and consumers are notified. </returns>
        private async Task ChangeAgentStateAsync(ConversationalAgentState newState)
        {
            var session = await this.agentSessionManager.GetSessionAsync();
            var oldState = session.AgentState;
            this.logger.Log(LogMessageLevel.ConversationalAgentSignal, $"Changing agent state: [{oldState.ToString()}] -> [{newState.ToString()}]");
            await session.RequestAgentStateChangeAsync(newState);
            this.DialogStateChanged?.Invoke(oldState, newState);
        }

        private void InitializeSignalDetectionHelper()
        {
            this.signalDetectionHelper = new SignalDetectionHelper(this.agentSessionManager);

            this.signalDetectionHelper.SignalReceived += async (DetectionOrigin detectionOrigin, bool signalNeedsVerification) =>
            {
                await this.StartConversationAsync(
                    detectionOrigin,
                    signalNeedsVerification);
            };

            this.signalDetectionHelper.SignalRejected += async (DetectionOrigin origin) =>
            {
                await this.dialogBackend?.CancelSignalVerificationAsync();
                await this.StopAudioCaptureAsync();
                this.logger.Log(LogMessageLevel.SignalDetection, $"Failsafe timer expired; rejecting");
                await this.FinishConversationAsync();
                this.SignalRejected.Invoke(origin);
            };

            this.signalDetectionHelper.SignalConfirmed += async (DetectionOrigin origin) =>
            {
                await this.ChangeAgentStateAsync(ConversationalAgentState.Listening);
                this.SignalConfirmed.Invoke(origin);
            };
        }

        private void OnKeywordRecognizing(string recognitionText)
        {
            this.signalDetectionHelper.KeywordRecognitionDuringSignalVerification(recognitionText, isFinal: false);
        }

        private async void OnKeywordRecognized(string recognitionText)
        {
            await this.dialogResponseQueue.AbortAsync();

            var session = await this.agentSessionManager.GetSessionAsync();

            if (session.AgentState == ConversationalAgentState.Listening)
            {
                await this.FinishConversationAsync();
                return;
            }

            this.signalDetectionHelper.KeywordRecognitionDuringSignalVerification(recognitionText, isFinal: true);
        }

        private void OnSpeechRecognizing(string recognitionText)
            => this.SpeechRecognizing?.Invoke(this, recognitionText);

        private async Task OnSpeechRecognizedAsync(string recognitionText)
        {
            await this.StopAudioCaptureAsync();
            await this.ChangeAgentStateAsync(ConversationalAgentState.Working);
            this.SpeechRecognized?.Invoke(this, recognitionText);
        }

        private void OnActivityReceived(DialogResponse dialogResponse)
        {
            if (!dialogResponse.TurnEndIndicated)
            {
                this.ConversationContinuationRequested = dialogResponse.FollowupTurnIndicated;
            }

            this.DialogResponseReceived?.Invoke(this, dialogResponse);
            this.dialogResponseQueue?.Enqueue(dialogResponse);
        }

        private async Task OnErrorReceivedAsync(DialogErrorInformation errorInformation)
        {
            this.logger.Log(LogMessageLevel.Error, $"DialogManager: error received: {errorInformation.ErrorDetails}");
            await this.ChangeAgentStateAsync(ConversationalAgentState.Inactive);
            await this.dialogResponseQueue.AbortAsync();
        }
    }
}
