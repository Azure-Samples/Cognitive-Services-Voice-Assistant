// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Dialog;
    using Newtonsoft.Json.Linq;
    using UWPVoiceAssistantSample.AudioInput;
    using UWPVoiceAssistantSample.KwsPerformance;
    using Windows.Storage;

    /// <summary>
    /// The implementation of the dialog backend for Direct Line Speech, using a
    /// DialogServiceConnector object for Azure speech service communication.
    /// </summary>
    public class DirectLineSpeechDialogBackend
        : IDialogBackend<List<byte>>
    {
        private static readonly TimeSpan KeywordRejectionTimeout = TimeSpan.FromSeconds(3.5);
        private readonly ILogProvider logger;
        private readonly KwsPerformanceLogger kwsPerformanceLogger;
        private readonly PullAudioInputSink audioIntoKeywordSink;
        private readonly PullAudioInputSink audioIntoConnectorSink;
        private IDialogAudioInputProvider<List<byte>> audioSource;
        private KeywordRecognizer keywordRecognizer;
        private AudioConfig keywordAudioConfig;
        private DialogServiceConnector connector;
        private AudioConfig connectorAudioConfig;
        private bool alreadyDisposed;
        private string speechKey;
        private string speechRegion;
        private string srLanguage;
        private string customSREndpointId;
        private string customVoiceDeploymentIds;
        private string customCommandsAppId;
        private Uri urlOverride;
        private string botId;
        private bool enableKwsLogging;
        private bool speechSdkLogEnabled;
        private bool startEventReceived;
        private bool secondStageConfirmed;
        private bool waitingForKeywordVerification;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectLineSpeechDialogBackend"/> class.
        /// </summary>
        public DirectLineSpeechDialogBackend()
        {
            this.logger = LogRouter.GetClassLogger();
            this.kwsPerformanceLogger = new KwsPerformanceLogger();

            void RejectKeyword(PullAudioInputSink sink, TimeSpan duration)
            {
                this.logger.Log($"{sink.Label} rejecting keyword "
                    + $"after processing {duration.TotalMilliseconds:0}ms of audio");
                this.KeywordRecognized?.Invoke(null);
            }

            this.audioIntoKeywordSink = new PullAudioInputSink()
            {
                Label = "Keyword Sink",
                DebugAudioFilesEnabled = LocalSettingsHelper.EnableAudioCaptureFiles,
                DataSource = PullAudioDataSource.PushedData,
                BookmarkPosition = KeywordRejectionTimeout,
            };
            this.audioIntoKeywordSink.BookmarkReached += (duration)
                => RejectKeyword(this.audioIntoKeywordSink, duration);

            this.audioIntoConnectorSink = new PullAudioInputSink()
            {
                Label = "Connector Sink",
                DebugAudioFilesEnabled = LocalSettingsHelper.EnableAudioCaptureFiles,
                BookmarkPosition = TimeSpan.Zero,
            };
            this.audioIntoConnectorSink.BookmarkReached += (duration)
                => RejectKeyword(this.audioIntoConnectorSink, duration);
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
        /// Sets up the initial state needed for Direct Line Speech, including creation of the
        /// underlying DialogServiceConnector and wiring of its events.
        /// </summary>
        /// <param name="keywordFile"> The keyword file to be loaded as part of initialization.</param>
        /// <returns> A task that completes once initialization is complete. </returns>
        public Task InitializeAsync(StorageFile keywordFile)
        {
            Contract.Requires(keywordFile != null);

            this.ConfirmationModel = KeywordRecognitionModel.FromFile(keywordFile.Path);

            if (this.keywordRecognizer == null)
            {
                this.InitializeKeywordRecognizer();
            }

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
            var activityJson = new JObject
            {
                ["type"] = "message",
                ["text"] = message,
            };
            return await this.connector.SendActivityAsync(activityJson.ToString());
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
                    if (this.audioIntoKeywordSink.DataSource == PullAudioDataSource.PushedData)
                    {
                        this.audioIntoKeywordSink.PushData(bytes);
                    }

                    if (this.audioIntoConnectorSink.DataSource == PullAudioDataSource.PushedData)
                    {
                        this.audioIntoConnectorSink.PushData(bytes);
                    }
                };
            }
        }

        /// <summary>
        /// Begins a new turn based on the input audio available from the provider.
        /// </summary>
        /// <param name="performConfirmation"> Whether keyword confirmation should be performed. </param>
        /// <returns> A task that completes immediately and does NOT block on start of turn. </returns>
        public Task StartAudioTurnAsync(bool performConfirmation)
        {
            if (!performConfirmation || this.ConfirmationModel == null)
            {
                this.audioIntoKeywordSink.DataSource = null;
                this.audioIntoConnectorSink.DataSource = PullAudioDataSource.PushedData;
                this.audioIntoConnectorSink.BookmarkPosition = TimeSpan.Zero;

                this.EnsureConnectorReady();
                _ = this.connector.ListenOnceAsync();
            }
            else
            {
                this.audioIntoKeywordSink.DataSource = PullAudioDataSource.PushedData;
                this.audioIntoKeywordSink.BookmarkPosition = KeywordRejectionTimeout;
                this.audioIntoConnectorSink.DataSource = null;

                var kwsModel = this.ConfirmationModel as KeywordRecognitionModel;
                _ = this.keywordRecognizer.RecognizeOnceAsync(kwsModel);
            }

            return Task.FromResult(0);
        }

        /// <summary>
        /// If the current turn is confirming a signal, abort the verfication.
        /// </summary>
        /// <returns> A task that completes when the in-progress turn has been aborted. </returns>
        public async Task CancelSignalVerificationAsync() => await this.StopAudioFlowAsync();

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
                    this.connectorAudioConfig?.Dispose();
                    this.audioIntoConnectorSink?.Dispose();
                    this.keywordRecognizer?.Dispose();
                    this.keywordAudioConfig?.Dispose();
                    this.audioIntoKeywordSink?.Dispose();
                }

                this.alreadyDisposed = true;
            }
        }

        private async Task StopAudioFlowAsync()
        {
            this.audioIntoKeywordSink.DataSource = PullAudioDataSource.EmptyInput;

            if (this.connector != null)
            {
                await this.connector.StopKeywordRecognitionAsync();
            }

            if (this.keywordRecognizer != null)
            {
                await this.keywordRecognizer.StopRecognitionAsync();
            }

            this.audioIntoKeywordSink.Reset();
            this.audioIntoConnectorSink.Reset();
        }

        private DialogServiceConfig CreateConnectorConfiguration()
        {
            DialogServiceConfig config;

            if (string.IsNullOrEmpty(this.speechKey) || string.IsNullOrEmpty(this.speechRegion))
            {
                throw new ArgumentException("Can't create a Connector configuration with no key/region!");
            }
            else if (!string.IsNullOrEmpty(this.customCommandsAppId))
            {
                config = CustomCommandsConfig.FromSubscription(this.customCommandsAppId, this.speechKey, this.speechRegion);
            }
            else if (!string.IsNullOrEmpty(this.botId))
            {
                config = BotFrameworkConfig.FromSubscription(this.speechKey, this.speechRegion, this.botId);
            }
            else
            {
                config = BotFrameworkConfig.FromSubscription(this.speechKey, this.speechRegion);
            }

            if (LocalSettingsHelper.AdditionalDialogProperties != null)
            {
                foreach (KeyValuePair<string, JToken> setPropertyId in LocalSettingsHelper.AdditionalDialogProperties)
                {
                    config.SetProperty(setPropertyId.Key, setPropertyId.Value.ToString());
                }
            }

            // Disable throttling of input audio (send it as fast as we can!)
            config.SetProperty("SPEECH-AudioThrottleAsPercentageOfRealTime", "9999");
            config.SetProperty("SPEECH-TransmitLengthBeforThrottleMs", "10000");

            var outputLabel = LocalSettingsHelper.OutputFormat.Label.ToLower(CultureInfo.CurrentCulture);
            config.SetProperty(PropertyId.SpeechServiceConnection_SynthOutputFormat, outputLabel);

            if (!string.IsNullOrEmpty(this.customSREndpointId))
            {
                config.SetServiceProperty("cid", this.customSREndpointId, ServicePropertyChannel.UriQueryParameter);

                // Custom Speech does not support Keyword Verification - Remove line below when supported.
                config.SetProperty("KeywordConfig_EnableKeywordVerification", "false");
            }

            if (!string.IsNullOrEmpty(this.customVoiceDeploymentIds))
            {
                config.SetProperty(PropertyId.Conversation_Custom_Voice_Deployment_Ids, this.customVoiceDeploymentIds);
            }

            if (this.speechSdkLogEnabled)
            {
                var logPath = $"{ApplicationData.Current.LocalFolder.Path}\\SpeechSDK.log";
                config.SetProperty(PropertyId.Speech_LogFilename, logPath);
            }

            return config;
        }

        private void InitializeKeywordRecognizer()
        {
            this.keywordAudioConfig = AudioConfig.FromStreamInput(this.audioIntoKeywordSink);

            // Disable throttling of input audio (send it as fast as we can!)
            this.keywordAudioConfig.SetProperty("SPEECH-AudioThrottleAsPercentageOfRealTime", "9999");
            this.keywordAudioConfig.SetProperty("SPEECH-TransmitLengthBeforThrottleMs", "10000");

            if (LocalSettingsHelper.SpeechSDKLogEnabled)
            {
                var logPath = $"{ApplicationData.Current.LocalFolder.Path}\\SpeechSDK.log";
                this.keywordAudioConfig.SetProperty(PropertyId.Speech_LogFilename, logPath);
            }

            this.keywordRecognizer = new KeywordRecognizer(this.keywordAudioConfig);

            this.keywordRecognizer.Recognized += (_, recoArgs) =>
            {
                this.logger.Log($"Keyword confirmed on device (@{this.audioIntoKeywordSink.AudioReadSinceReset.TotalMilliseconds:0}ms): {recoArgs.Result.Text}");
                this.audioIntoKeywordSink.BookmarkPosition = TimeSpan.Zero;
                this.KeywordRecognizing?.Invoke(recoArgs.Result.Text);
                this.audioIntoConnectorSink.DataSource = PullAudioDataSource.FromKeywordResult(recoArgs.Result);
                this.audioIntoConnectorSink.BookmarkPosition = KeywordRejectionTimeout;
                this.EnsureConnectorReady();
                this.logger.Log($"Starting connector");
                _ = this.connector.StartKeywordRecognitionAsync(this.ConfirmationModel as KeywordRecognitionModel);
            };
        }

        private void EnsureConnectorReady()
        {
            if (this.IsConnectorConfigurationUpToDate() && this.connector != null)
            {
                // The connector is already initialized and up to date.
                return;
            }

            this.connector?.Dispose();
            this.connectorAudioConfig?.Dispose();
            this.connectorAudioConfig = AudioConfig.FromStreamInput(this.audioIntoConnectorSink);

            this.connector = new DialogServiceConnector(
                this.CreateConnectorConfiguration(),
                this.connectorAudioConfig);

            this.connector.SessionStarted += (_, e) =>
            {
                this.waitingForKeywordVerification = false;
                this.SessionStarted?.Invoke(e.SessionId);
            };
            this.connector.SessionStopped += (_, e) => this.SessionStopped?.Invoke(e.SessionId);
            this.connector.Recognizing += (_, e) => this.OnConnectorRecognizing(e.Result);
            this.connector.Recognized += async (s, e) => await this.OnConnectorRecognizedAsync(e.Result);
            this.connector.Canceled += async (s, e) => await this.OnConnectorCanceledAsync(e);
            this.connector.ActivityReceived += (s, e) => this.OnConnectorActivityReceived(e);
        }

        private void OnConnectorRecognizing(SpeechRecognitionResult result)
        {
            switch (result.Reason)
            {
                case ResultReason.RecognizingKeyword:
                    this.audioIntoConnectorSink.BookmarkPosition = TimeSpan.Zero;
                    this.waitingForKeywordVerification = true;
                    break;
                case ResultReason.RecognizingSpeech:
                    this.logger.Log(LogMessageLevel.SignalDetection, $"Recognized speech in progress: \"{result.Text}\"");
                    this.SpeechRecognizing?.Invoke(result.Text);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private async Task OnConnectorRecognizedAsync(SpeechRecognitionResult result)
        {
            KwsPerformanceLogger.KwsEventFireTime = TimeSpan.FromTicks(DateTime.Now.Ticks);
            switch (result.Reason)
            {
                case ResultReason.RecognizedKeyword:
                    this.waitingForKeywordVerification = false;
                    var thirdStageStartTime = KwsPerformanceLogger.KwsStartTime.Ticks;
                    thirdStageStartTime = DateTime.Now.Ticks;
                    this.logger.Log(LogMessageLevel.SignalDetection, $"Cloud model recognized keyword \"{result.Text}\"");
                    this.KeywordRecognized?.Invoke(result.Text);
                    this.kwsPerformanceLogger.LogSignalReceived("SWKWS", "A", "3", KwsPerformanceLogger.KwsEventFireTime.Ticks, thirdStageStartTime, DateTime.Now.Ticks);
                    this.secondStageConfirmed = false;
                    break;
                case ResultReason.RecognizedSpeech:
                    this.logger.Log(LogMessageLevel.SignalDetection, $"Recognized final speech: \"{result.Text}\"");
                    this.SpeechRecognized?.Invoke(result.Text);
                    await this.StopAudioFlowAsync();
                    break;
                case ResultReason.NoMatch:
                    // If a KeywordRecognized handler is available, this is a final stage
                    // keyword verification rejection.
                    if (this.waitingForKeywordVerification)
                    {
                        this.logger.Log(LogMessageLevel.SignalDetection, $"Cloud model rejected keyword");
                        if (this.secondStageConfirmed)
                        {
                            var thirdStageStartTimeRejected = KwsPerformanceLogger.KwsStartTime.Ticks;
                            thirdStageStartTimeRejected = DateTime.Now.Ticks;
                            this.kwsPerformanceLogger.LogSignalReceived("SWKWS", "R", "3", KwsPerformanceLogger.KwsEventFireTime.Ticks, thirdStageStartTimeRejected, DateTime.Now.Ticks);
                            this.secondStageConfirmed = false;
                        }

                        this.KeywordRecognized?.Invoke(null);
                        await this.StopAudioFlowAsync();
                    }
                    else
                    {
                        this.SpeechRecognized?.Invoke(string.Empty);
                    }

                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private async Task OnConnectorCanceledAsync(SpeechRecognitionCanceledEventArgs canceledArgs)
        {
            if (canceledArgs.Reason == CancellationReason.EndOfStream)
            {
                // End of streams are expected and uninteresting with normal Start/Stop management
            }
            else
            {
                var code = (int)canceledArgs.ErrorCode;
                var message = $"{canceledArgs.Reason}: {canceledArgs.ErrorDetails}";
                await this.StopAudioFlowAsync();
                this.ErrorReceived?.Invoke(new DialogErrorInformation(code, message));
            }
        }

        private void OnConnectorActivityReceived(ActivityReceivedEventArgs activityArgs)
        {
            // Note: the contract of when to end a turn is unique to your dialog system. In this sample,
            // it's assumed that receiving a message activity without audio marks the end of a turn. Your
            // dialog system may have a different contract!
            var wrapper = new ActivityWrapper(activityArgs.Activity);

            if (wrapper.Type == ActivityWrapper.ActivityType.Event)
            {
                if (!this.startEventReceived)
                {
                    this.startEventReceived = true;
                    return;
                }
                else
                {
                    this.startEventReceived = false;
                }
            }

            var messageMedia = activityArgs.HasAudio ?
                new DirectLineSpeechAudioOutputStream(activityArgs.Audio, LocalSettingsHelper.OutputFormat)
                : null;
            var shouldEndTurn = (activityArgs.Audio == null && wrapper.Type == ActivityWrapper.ActivityType.Message)
                || wrapper.Type == ActivityWrapper.ActivityType.Event;
            var payload = new DialogResponse(
                messageBody: activityArgs.Activity,
                messageMedia,
                shouldEndTurn,
                shouldStartNewTurn: wrapper.InputHint == ActivityWrapper.InputHintType.ExpectingInput);

            this.DialogResponseReceived?.Invoke(payload);
        }

        private bool IsConnectorConfigurationUpToDate()
        {
            var speechKey = LocalSettingsHelper.SpeechSubscriptionKey;
            var speechRegion = LocalSettingsHelper.SpeechRegion;
            var srLanguage = LocalSettingsHelper.SRLanguage;
            var customSREndpointId = LocalSettingsHelper.CustomSREndpointId;
            var customVoiceDeploymentIds = LocalSettingsHelper.CustomVoiceDeploymentIds;
            var customCommandsAppId = LocalSettingsHelper.CustomCommandsAppId;
            var urlOverride = LocalSettingsHelper.UrlOverride;
            var botId = LocalSettingsHelper.BotId;
            var speechSdkLogEnabled = LocalSettingsHelper.SpeechSDKLogEnabled;
            var enableKwsLogging = LocalSettingsHelper.EnableKwsLogging;

            if (this.speechKey == speechKey
                && this.speechRegion == speechRegion
                && this.srLanguage == srLanguage
                && this.customSREndpointId == customSREndpointId
                && this.customVoiceDeploymentIds == customVoiceDeploymentIds
                && this.customCommandsAppId == customCommandsAppId
                && this.urlOverride == urlOverride
                && this.botId == botId
                && this.speechSdkLogEnabled == speechSdkLogEnabled
                && this.enableKwsLogging == enableKwsLogging)
            {
                return true;
            }

            this.speechKey = speechKey;
            this.speechRegion = speechRegion;
            this.srLanguage = srLanguage;
            this.customSREndpointId = customSREndpointId;
            this.customVoiceDeploymentIds = customVoiceDeploymentIds;
            this.customCommandsAppId = customCommandsAppId;
            this.urlOverride = urlOverride;
            this.botId = botId;
            this.speechSdkLogEnabled = speechSdkLogEnabled;
            this.enableKwsLogging = enableKwsLogging;

            return false;
        }
    }
}
