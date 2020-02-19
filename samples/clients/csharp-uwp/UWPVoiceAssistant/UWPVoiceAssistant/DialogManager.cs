// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
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
    public class DialogManager : IDisposable
    {
        private static readonly SemaphoreSlim InstanceSemaphore = new SemaphoreSlim(1, 1);
        private static IDialogBackend dialogInstanceBackend = null;
        private static bool dialogBackendChanged = false;
        private static DialogManager dialogManagerInstance = null;

        private IDialogBackend dialogBackend;
        private IDialogAudioInputProvider dialogAudioInput;
        private DialogAudioOutputAdapter dialogAudioOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogManager"/> class.
        /// </summary>
        /// <param name="dialogBackend"> The dialog backend for the manager to use. </param>
        /// <param name="dialogAudioOutput"> The dialog audio output sink to use. </param>
        protected DialogManager(
            IDialogBackend dialogBackend,
            DialogAudioOutputAdapter dialogAudioOutput)
        {
            Contract.Requires(dialogBackend != null);
            this.dialogBackend = dialogBackend;
            this.dialogBackend.SessionStarted += (id)
                => Debug.WriteLine($"DialogManager: Session start: {id}");
            this.dialogBackend.SessionStopped += (id)
                => Debug.WriteLine($"DialogManager: Session stop: {id}");
            this.dialogBackend.KeywordRecognizing += this.OnKeywordRecognizing;
            this.dialogBackend.KeywordRecognized += this.OnKeywordRecognized;
            this.dialogBackend.SpeechRecognizing += this.OnSpeechRecognizing;
            this.dialogBackend.SpeechRecognized += async (text)
                => await this.OnSpeechRecognizedAsync(text);
            this.dialogBackend.DialogResponseReceived += this.OnActivityReceived;
            this.dialogBackend.ErrorReceived += async (errorInformation)
                => await this.OnErrorReceivedAsync(errorInformation);
            this.dialogAudioInput = null;
            this.dialogAudioOutput = dialogAudioOutput;
            if (this.dialogAudioOutput != null)
            {
                this.dialogAudioOutput.OutputEnded += async () =>
                {
                    var session = await AppSharedState.GetSessionAsync();
                    if (session.AgentState == ConversationalAgentState.Speaking)
                    {
                        await this.ChangeAgentStateAsync(ConversationalAgentState.Inactive);
                    }
                };
            }
        }

        /// <summary>
        /// Raised when the state machine for conversational agent state has finished setting
        /// a new state after requests to or responses from the dialog backend have required it.
        /// </summary>
        public event DialogStateChangeEventArgs DialogStateChanged;

        /// <summary>
        /// Raised when an intermediate stage of keyword recognition or verification has been
        /// performed by the backend. Assistant implementations may choose to take staged user-
        /// perceivable action across multiple layers of confirmation or ignore intermediate stages
        /// and act only on the final, fully-confirmed recognition.
        /// </summary>
        public event EventHandler<string> KeywordRecognizing;

        /// <summary>
        /// Raised when the final keyword recognition step has been performed by the backend. An
        /// accepted result at this stage is full confirmation that user-perceived action, like
        /// showing a window and playing an earcon, is appropriate.
        /// </summary>
        public event EventHandler<string> KeywordRecognized;

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
        /// Gets or sets the dialog backend implementation used to instantiate the singleton created
        /// and stored in GetInstanceAsync().
        /// </summary>
        public static IDialogBackend InstanceBackend
        {
            get => DialogManager.dialogInstanceBackend ?? new DirectLineSpeechDialogBackend();
            set
            {
                DialogManager.dialogInstanceBackend = value;
                DialogManager.dialogBackendChanged = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether if connection to DLS or Mock Client needs to be recreated.
        /// </summary>
        public bool ShouldReinitialize { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the current conversation is requesting that it
        /// be continued after the latest turn completes.
        /// </summary>
        public bool ConversationContinuationRequested { get; set; } = false;

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
        /// Asynchronously creates and fetches a singleton instance of a DialogManager with the configured
        /// dialog backend.
        /// </summary>
        /// <returns> A task that completes when the manager is available. </returns>
        public static async Task<DialogManager> GetInstanceAsync()
        {
            using (await DialogManager.InstanceSemaphore.AutoReleaseWaitAsync())
            {
                if (DialogManager.dialogManagerInstance == null || DialogManager.dialogBackendChanged)
                {
                    DialogManager.dialogManagerInstance = await DialogManager.CreateAsync(
                        DialogManager.InstanceBackend);
                    DialogManager.dialogBackendChanged = false;
                }

                return DialogManager.dialogManagerInstance;
            }
        }

        /// <summary>
        /// Instructs the manager to update the current ConversationalAgentSessionState to the
        /// requested value and inform consumers of the change.
        /// Indicates ConversationalSessionState changes based on requests and responses to and from the Bot.
        /// </summary>
        /// <param name="newState"> The new, requested conversational agent state. </param>
        /// <returns> A task that completes once the state is updated and consumers are notified. </returns>
        public async Task ChangeAgentStateAsync(ConversationalAgentState newState)
        {
            var session = await AppSharedState.GetSessionAsync();
            var oldState = session.AgentState;
            Debug.WriteLine($"Changing agent state: [{oldState.ToString()}] -> [{newState.ToString()}]");
            await session.RequestAgentStateChangeAsync(newState);
            this.DialogStateChanged?.Invoke(oldState, newState);
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
        }

        /// <summary>
        /// Sends an activity as defined by a derived class.
        /// </summary>
        /// <param name="activityJson"> The activity, encoded as json. </param>
        /// <returns> A task that completes once the activity is sent. </returns>
        public async Task<string> SendActivityAsync(string activityJson)
        {
            var id = await this.dialogBackend.SendDialogMessageAsync(activityJson);
            return id;
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
                Debug.WriteLine($"DialogManager2::SetupConversationAsync didn't succeed in setting up a conversation (see earlier errors). Aborting conversation.");
                return;
            }

            await this.StartTurnAsync(signalVerificationRequired);
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
        /// This code added to correctly implement the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously creates an instance of the DialogManager class that includes an initialized audio output
        /// adapter for dialog audio.
        /// </summary>
        /// <param name="dialogBackend"> The dialog backend to use with the manager. </param>
        /// <returns> A task that completes once the manager and its underlying resources are ready. </returns>
        protected static async Task<DialogManager> CreateAsync(IDialogBackend dialogBackend)
        {
            var audioOutput = await DialogAudioOutputAdapter.CreateAsync();
            var manager = new DialogManager(dialogBackend, audioOutput);
            return manager;
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
            var session = await AppSharedState.GetSessionAsync();
            try
            {
                this.dialogAudioInput = signalOrigin == DetectionOrigin.FromPushToTalk
                    ? await AgentAudioProducer.FromNowAsync()
                    : await AgentAudioProducer.FromAgentSessionAsync(session);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to acquire MVA 1st-pass audio. Rejecting signal.\n{ex.HResult}: {ex.Message}");
                await this.FinishConversationAsync();
                return false;
            }

            this.dialogBackend.SetAudioSource(this.dialogAudioInput);
            await this.dialogBackend.InitializeAsync();

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
            var newState = signalVerificationRequired
                ? ConversationalAgentState.Detecting
                : ConversationalAgentState.Listening;
            await this.ChangeAgentStateAsync(newState);

            await this.dialogBackend.StartAudioTurnAsync(signalVerificationRequired);

            var audioToSkip = signalVerificationRequired
                ? AgentAudioProducer.InitialKeywordTrimDuration
                : TimeSpan.Zero;
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
                    await this.dialogAudioOutput?.StopPlaybackAsync();
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

        private void OnKeywordRecognizing(string recognitionText)
        {
            this.KeywordRecognizing?.Invoke(this, recognitionText);
        }

        private void OnKeywordRecognized(string recognitionText)
            => this.KeywordRecognized?.Invoke(this, recognitionText);

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
            this.ConversationContinuationRequested = dialogResponse.FollowupTurnIndicated;

            if (dialogResponse.MessageMedia != null)
            {
                _ = this.ChangeAgentStateAsync(ConversationalAgentState.Speaking);
                this.dialogAudioOutput.EnqueueDialogAudio(dialogResponse.MessageMedia);
            }

            this.DialogResponseReceived?.Invoke(this, dialogResponse);

            if (dialogResponse.TurnEndIndicated)
            {
                _ = this.FinishTurnAsync();
            }
        }

        private async Task OnErrorReceivedAsync(DialogErrorInformation errorInformation)
        {
            Debug.WriteLine($"DialogManager: error received: {errorInformation.ErrorDetails}");
            await this.ChangeAgentStateAsync(ConversationalAgentState.Inactive);
        }
    }
}
