// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Dialog;
    using UWPVoiceAssistantSample.AudioCommon;
    using UWPVoiceAssistantSample.AudioOutput;
    using Windows.Media.MediaProperties;
    using Windows.Storage;

    /// <summary>
    /// The implementation of the dialog backend for Direct Line Speech, using a
    /// DialogServiceConnector object for Azure speech service communication.
    /// </summary>
    public class DirectLineSpeechDialogBackend
        : IDialogBackend<List<byte>>
    {
        private IDialogAudioInputProvider<List<byte>> audioSource;
        private DialogServiceConnector connector;
        private PushAudioInputStream connectorInputStream;
        private bool alreadyDisposed = false;
        private ILogProvider logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectLineSpeechDialogBackend"/> class.
        /// </summary>
        public DirectLineSpeechDialogBackend()
        {
            this.logger = LogRouter.GetClassLogger();
        }

        /// <summary>
        /// Raised when audio has begun flowing to Direct Line Speech and returns the interaction
        /// ID associated with the session.
        /// </summary>
        public event Action<string> SessionStarted;

        /// <summary>
        /// Raised when audio has stopped flowing to Direct Line Speech and returns the interaction
        /// ID associated with the session.
        /// </summary>
        public event Action<string> SessionStopped;

        /// <summary>
        /// Raised when Direct Line Speech provides a speech-to-text result with the
        /// KeywordRecognizing reason specified, indicating that a non-final stage of keyword
        /// matching and verification has occurred.
        /// </summary>
        public event Action<string> KeywordRecognizing;

        /// <summary>
        /// Raised when Direct Line Speech has completed all stages of keyword matching and
        /// verification.
        /// </summary>
        public event Action<string> KeywordRecognized;

        /// <summary>
        /// Raised when Direct Line Speech provides a non-final speech-to-text result for the
        /// ongoing speech recognition against input audio.
        /// </summary>
        public event Action<string> SpeechRecognizing;

        /// <summary>
        /// Raised when Direct Line Speech provides a final speech-to-text result for the
        /// ongoing speech recognition against input audio.
        /// </summary>
        public event Action<string> SpeechRecognized;

        /// <summary>
        /// Raised when Direct Line Speech receives an activity from its backing bot or
        /// application.
        /// </summary>
        public event Action<DialogResponse> DialogResponseReceived;

        /// <summary>
        /// Raised when Direct Line Speech produces a Canceled event from its underlying
        /// DialogServiceConnector, which typically indicates an error.
        /// </summary>
        public event Action<DialogErrorInformation> ErrorReceived;

        /// <summary>
        /// Gets or sets the 2nd-stage keyword model (file.table) used with Direct Line Speech.
        /// </summary>
        public object ConfirmationModel { get; set; }

        /// <summary>
        /// Gets or sets the configuration object that will be used when creating the
        /// DialogServiceConnector object for Direct Line Speech.
        /// </summary>
        public DialogServiceConfig ConnectorConfiguration { get; set; }

        /// <summary>
        /// Sets up the initial state needed for Direct Line Speech, including creation of the
        /// underlying DialogServiceConnector and wiring of its events.
        /// </summary>
        /// <param name="keywordFile"> The keyword file to be loaded as part of initialization.</param>
        /// <returns> A task that completes once initialization is complete. </returns>
        public Task InitializeAsync(StorageFile keywordFile)
        {
            Contract.Requires(keywordFile != null);

            // Default values -- these can be updated
            this.ConnectorConfiguration = this.CreateConfiguration();
            this.ConfirmationModel = KeywordRecognitionModel.FromFile(keywordFile.Path);

            this.connectorInputStream = AudioInputStream.CreatePushStream();
            this.connector = new DialogServiceConnector(
                this.ConnectorConfiguration,
                AudioConfig.FromStreamInput(this.connectorInputStream));

            this.connector.SessionStarted += (s, e) => this.SessionStarted?.Invoke(e.SessionId);
            this.connector.SessionStopped += (s, e) => this.SessionStopped?.Invoke(e.SessionId);
            this.connector.Recognizing += (s, e) =>
            {
                switch (e.Result.Reason)
                {
                    case ResultReason.RecognizingKeyword:
                        this.logger.Log($"Local model recognized keyword \"{e.Result.Text}\"");
                        this.KeywordRecognizing?.Invoke(e.Result.Text);
                        break;
                    case ResultReason.RecognizingSpeech:
                        this.logger.Log($"Recognized speech in progress: \"{e.Result.Text}\"");
                        this.SpeechRecognizing?.Invoke(e.Result.Text);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            };
            this.connector.Recognized += (s, e) =>
            {
                switch (e.Result.Reason)
                {
                    case ResultReason.RecognizedKeyword:
                        this.logger.Log($"Cloud model recognized keyword \"{e.Result.Text}\"");
                        this.KeywordRecognized?.Invoke(e.Result.Text);
                        break;
                    case ResultReason.RecognizedSpeech:
                        this.logger.Log($"Recognized final speech: \"{e.Result.Text}\"");
                        this.SpeechRecognized?.Invoke(e.Result.Text);
                        break;
                    case ResultReason.NoMatch:
                        // If a KeywordRecognized handler is available, this is a final stage
                        // keyword verification rejection.
                        this.logger.Log($"Cloud model rejected keyword");
                        this.KeywordRecognized?.Invoke(null);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            };
            this.connector.Canceled += (s, e) =>
            {
                var code = (int)e.ErrorCode;
                var message = $"{e.Reason.ToString()}: {e.ErrorDetails}";
                this.ErrorReceived?.Invoke(new DialogErrorInformation(code, message));
            };
            this.connector.ActivityReceived += (s, e) =>
            {
                // Note: the contract of when to end a turn is unique to your dialog system. In this sample,
                // it's assumed that receiving a message activity without audio marks the end of a turn. Your
                // dialog system may have a different contract!
                var wrapper = new ActivityWrapper(e.Activity);
                var payload = new DialogResponse(
                    messageBody: e.Activity,
                    messageMedia: e.HasAudio ? new DirectLineSpeechAudioOutputStream(e.Audio, LocalSettingsHelper.OutputFormat) : null,
                    shouldEndTurn: e.Audio == null && wrapper.Type == ActivityWrapper.ActivityType.Message,
                    shouldStartNewTurn: wrapper.InputHint == ActivityWrapper.InputHintType.ExpectingInput);
                this.logger.Log($"Connector activity received");
                this.DialogResponseReceived?.Invoke(payload);
            };

            return Task.FromResult(0);
        }

        /// <summary>
        /// Sends an activity to Direct Line Speech for asynchronous processing.
        /// </summary>
        /// <param name="message"> The message (e.g. JSON) for the activity. </param>
        /// <returns>
        ///     A task that completes once the message is sent. Provides the id associated with
        ///     the message request.
        /// </returns>
        public async Task<string> SendDialogMessageAsync(string message)
        {
            var id = await this.connector.SendActivityAsync(message);
            return id;
        }

        /// <summary>
        /// Sets the audio source to be used by this dialog backend and registers its data
        /// for use.
        /// </summary>
        /// <param name="source"> The agent audio source to use. </param>
        public void SetAudioSource(IDialogAudioInputProvider<List<byte>> source)
        {
            if (this.audioSource != source)
            {
                Contract.Requires(source != null);

                this.audioSource = source;
                this.audioSource.DataAvailable += (bytes) =>
                {
                    this.connectorInputStream?.Write(bytes.ToArray());
                };
            }
        }

        /// <summary>
        /// Begins a new turn based on the input audio available from the provider.
        /// </summary>
        /// <param name="performConfirmation"> Whether keyword confirmation should be performed. </param>
        /// <returns> A task that completes immediately and does NOT block on start of turn. </returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task StartAudioTurnAsync(bool performConfirmation)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (!performConfirmation || this.ConfirmationModel == null)
            {
                _ = this.connector.ListenOnceAsync();
            }
            else
            {
                var kwsModel = this.ConfirmationModel as KeywordRecognitionModel;
                _ = this.connector.StartKeywordRecognitionAsync(kwsModel);
            }
        }

        /// <summary>
        /// If the current turn is confirming a signal, abort the verfication.
        /// </summary>
        /// <returns> A task that completes when the in-progress turn has been aborted. </returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task CancelSignalVerification()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            _ = this.connector.StopKeywordRecognitionAsync();
        }

        /// <summary>
        /// Basic implementation of IDisposable pattern.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Basic implementation of IDisposable pattern.
        /// </summary>
        /// <param name="disposing"> Whether managed resource disposal is happening. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.alreadyDisposed)
            {
                if (disposing)
                {
                    this.connector?.Dispose();
                    this.connectorInputStream?.Dispose();
                }

                this.alreadyDisposed = true;
            }
        }

        private DialogServiceConfig CreateConfiguration()
        {
            var speechKey = LocalSettingsHelper.SpeechSubscriptionKey;
            var speechRegion = LocalSettingsHelper.AzureRegion;
            var customSpeechId = LocalSettingsHelper.CustomSpeechId;
            var customVoiceIds = LocalSettingsHelper.CustomVoiceIds;
            var customCommandsAppId = LocalSettingsHelper.CustomCommandsAppId;
            var botId = LocalSettingsHelper.BotId;

            // Subscription information is supported in multiple formats:
            //  <subscription_key>     use the default bot associated with the subscription
            //  <sub_key>:<app_id>     use a specified Custom Commands application
            //  <sub_key>#<bot_id>     use a specific bot within the subscription
            DialogServiceConfig config;

            if (!string.IsNullOrEmpty(speechKey) && !string.IsNullOrEmpty(speechRegion) && !string.IsNullOrEmpty(customCommandsAppId))
            {
                config = CustomCommandsConfig.FromSubscription(customCommandsAppId, speechKey, speechRegion);
            }

            // else if (!string.IsNullOrEmpty(speechKey) && !string.IsNullOrEmpty(speechRegion) && !string.IsNullOrEmpty(botId))
            // {
            //    config = BotFrameworkConfig.FromSubscription(speechKey, speechRegion, botId);
            // }
            else
            {
                config = BotFrameworkConfig.FromSubscription(
                    speechKey,
                    speechRegion);
            }

            // Disable throttling of input audio (send it as fast as we can!)
            config.SetProperty("SPEECH-AudioThrottleAsPercentageOfRealTime", "9999");
            config.SetProperty("SPEECH-TransmitLengthBeforThrottleMs", "10000");

            var outputLabel = LocalSettingsHelper.OutputFormat.Label.ToLower(CultureInfo.CurrentCulture);
            config.SetProperty(PropertyId.SpeechServiceConnection_SynthOutputFormat, outputLabel);

            if (!string.IsNullOrEmpty(customSpeechId))
            {
                config.SetServiceProperty("cid", customSpeechId, ServicePropertyChannel.UriQueryParameter);

                // Custom Speech does not support Keyword Verification - Remove line below when supported.
                config.SetProperty("KeywordConfig_EnableKeywordVerification", "false");
            }

            if (!string.IsNullOrEmpty(customVoiceIds))
            {
                config.SetProperty(PropertyId.Conversation_Custom_Voice_Deployment_Ids, customVoiceIds);
            }

            if (LocalSettingsHelper.EnableSdkLogging)
            {
                var logPath = $"{ApplicationData.Current.LocalFolder.Path}\\sdklog.txt";
                config.SetProperty(PropertyId.Speech_LogFilename, logPath);
            }

            return config;
        }
    }
}
