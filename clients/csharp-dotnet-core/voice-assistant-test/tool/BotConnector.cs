// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.CognitiveServices.Speech.Dialog;
    using NAudio.Wave;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Activity = Microsoft.Bot.Schema.Activity;
    using IMessageActivity = Microsoft.Bot.Schema.IMessageActivity;

    /// <summary>
    /// Manages the Connection and Responses to and from the Bot using the DialogServiceConnector object.
    /// </summary>
    internal class BotConnector : IDisposable
    {
        private const int MaxSizeOfTtsAudioInBytes = 65536;
        private const int WavHeaderSizeInBytes = 44;
        private const int BytesToRead = 3200;
        private const uint ResponseCheckInterval = 100; // milliseconds
        private int responseCount;
        private int timeout;
        private string outputWAV;
        private DialogServiceConnector connector = null;
        private PushAudioInputStream pushAudioInputStream = null;
        private AppSettings appsettings;
        private string baseFileName;
        private string dialogID;
        private int turnID;
        private int indexActivityWithAudio = 0;
        private int ttsStreamDownloadCount = 0;
        private List<Activity> ignoreActivitiesList;
        private Stopwatch stopWatch;
        private bool keyword;
        private KeywordRecognitionModel kwsTable;
        private int speechDuration = 0;
        private AudioConfig audioConfig = AudioConfig.FromStreamInput(GlobalPullStream.FilePullStreamCallback);

        /// <summary>
        /// Gets or sets recognized text of the speech input.
        /// </summary>
        public string RecognizedText { get; set; }

        /// <summary>
        /// Gets or sets recognized keyword.
        /// </summary>
        public string RecognizedKeyword { get; set; }

        private List<BotReply> BotReplyList { get; set; }

        /// <summary>
        /// Initializes the connection to the Bot.
        /// </summary>
        /// <param name="settings">Application settings object, built from the input JSON file supplied as run-time argument.</param>
        public void InitConnector(AppSettings settings)
        {
            DialogServiceConfig config;
            this.BotReplyList = new List<BotReply>();
            this.stopWatch = new Stopwatch();
            this.appsettings = settings;

            if (!string.IsNullOrWhiteSpace(this.appsettings.CustomCommandsAppId))
            {
                // NOTE: Custom commands is a preview Azure Service.
                // Set the custom commands configuration object based on three items:
                // - The Custom commands application ID
                // - Cognitive services speech subscription key.
                // - The Azure region of the subscription key(e.g. "westus").
                config = CustomCommandsConfig.FromSubscription(this.appsettings.CustomCommandsAppId, this.appsettings.SpeechSubscriptionKey, this.appsettings.SpeechRegion);
            }
            else
            {
                // Set the bot framework configuration object based on two items:
                // - Cognitive services speech subscription key. It is needed for billing and is tied to the bot registration.
                // - The Azure region of the subscription key(e.g. "westus").
                config = BotFrameworkConfig.FromSubscription(this.appsettings.SpeechSubscriptionKey, this.appsettings.SpeechRegion);
            }

            if (this.appsettings.SpeechSDKLogEnabled)
            {
                // Speech SDK has verbose logging to local file, which may be useful when reporting issues.
                config.SetProperty(PropertyId.Speech_LogFilename, $"{this.appsettings.OutputFolder}SpeechSDKLog-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.CurrentCulture)}.log");
            }

            if (!string.IsNullOrWhiteSpace(this.appsettings.SRLanguage))
            {
                // Set the speech recognition language. If not set, the default is "en-us".
                config.Language = this.appsettings.SRLanguage;
            }

            if (!string.IsNullOrWhiteSpace(this.appsettings.CustomSREndpointId))
            {
                // Set your custom speech end-point id here, as given to you by the speech portal https://speech.microsoft.com/portal.
                // Otherwise the standard speech end-point will be used.
                config.SetServiceProperty("cid", this.appsettings.CustomSREndpointId, ServicePropertyChannel.UriQueryParameter);

                // Custom Speech does not support cloud Keyword Verification at the moment. If this is not done, there will be an error
                // from the service and connection will close. Remove line below when supported.
                config.SetProperty("KeywordConfig_EnableKeywordVerification", "false");
            }
            else
            {
                // If a custom speech endpoint is not specified, keyword verification is set
                // according to the default or configured settings.
                config.SetProperty("KeywordConfig_EnableKeywordVerification", this.appsettings.KeywordVerificationEnabled.ToString().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(this.appsettings.CustomVoiceDeploymentIds))
            {
                // Set one or more IDs associated with the custom TTS voice your bot will use.
                // The format of the string is one or more GUIDs separated by comma (no spaces). You get these GUIDs from
                // your custom TTS on the speech portal https://speech.microsoft.com/portal.
                config.SetProperty(PropertyId.Conversation_Custom_Voice_Deployment_Ids, this.appsettings.CustomVoiceDeploymentIds);
            }

            this.timeout = this.appsettings.Timeout;

            if (!string.IsNullOrWhiteSpace(this.appsettings.KeywordRecognitionModel))
            {
                this.kwsTable = KeywordRecognitionModel.FromFile(this.appsettings.KeywordRecognitionModel);
            }

            if (this.appsettings.SetPropertyId != null)
            {
                foreach (KeyValuePair<string, JToken> setPropertyIdPair in this.appsettings.SetPropertyId)
                {
                    config.SetProperty(setPropertyIdPair.Key, setPropertyIdPair.Value.ToString());
                }
            }

            if (this.appsettings.SetPropertyString != null)
            {
                foreach (KeyValuePair<string, JToken> setPropertyStringPair in this.appsettings.SetPropertyString)
                {
                    config.SetProperty(setPropertyStringPair.Key.ToString(CultureInfo.CurrentCulture), setPropertyStringPair.Value.ToString());
                }
            }

            if (this.appsettings.SetServiceProperty != null)
            {
                foreach (KeyValuePair<string, JToken> setServicePropertyPair in this.appsettings.SetServiceProperty)
                {
                    config.SetServiceProperty(setServicePropertyPair.Key.ToString(CultureInfo.CurrentCulture), setServicePropertyPair.Value.ToString(), ServicePropertyChannel.UriQueryParameter);
                }
            }

            if (this.appsettings.RealTimeAudio)
            {
                config.SetProperty("SPEECH-AudioThrottleAsPercentageOfRealTime", "100");
                config.SetProperty("SPEECH-TransmitLengthBeforeThrottleMs", "0");
            }

            if (this.connector != null)
            {
                // Then dispose the object
                this.connector.Dispose();
                this.connector = null;
            }

            if (this.appsettings.PushStreamEnabled)
            {
                this.pushAudioInputStream = AudioInputStream.CreatePushStream();
                this.audioConfig = AudioConfig.FromStreamInput(this.pushAudioInputStream);
            }
            else
            {
                config.SetProperty("KeywordConfig_EnableKeywordVerification", "false");
                this.audioConfig = AudioConfig.FromStreamInput(GlobalPullStream.FilePullStreamCallback);
            }

            this.connector = new DialogServiceConnector(config, this.audioConfig);
            if (this.appsettings.BotGreeting)
            {
                // Starting the timer to calculate latency for Bot Greeting.
                this.stopWatch.Restart();
            }

            this.AttachHandlers();
        }

        /// <summary>
        /// Connects to the Bot using the DialogServiceConnector object.
        /// </summary>
        /// <returns>Connection to Bot.</returns>
        public async Task Connect()
        {
            if (this.connector != null)
            {
                await this.connector.ConnectAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Disconnects from the Bot.
        /// </summary>
        /// <returns>Disconnection.</returns>
        public async Task Disconnect()
        {
            if (this.connector != null)
            {
                this.DetachHandlers();
                await this.connector.DisconnectAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Calls StartKeywordRecognitionAsync.
        /// </summary>
        /// <returns>Opens audio stream with Keyword Recognition Model.</returns>
        public async Task StartKeywordRecognitionAsync()
        {
            await this.connector.StartKeywordRecognitionAsync(this.kwsTable).ConfigureAwait(false);
        }

        /// <summary>
        /// Calls StopKeywordRecognitionAsync.
        /// </summary>
        /// <returns>Closes audio stream started by StartKeywordRecognitionAsync.</returns>
        public async Task StopKeywordRecognitionAsync()
        {
            await this.connector.StopKeywordRecognitionAsync().ConfigureAwait(false);
            Thread.Sleep(1000);
        }

        /// <summary>
        /// Sends a message activity to the bot after wrapping a message in an Activity object.
        /// </summary>
        /// <param name="message">Utterance text in each turn.</param>
        /// <returns>Activity.</returns>
        public async Task<BotConnector> Send(string message)
        {
            IMessageActivity bfActivity = Activity.CreateMessageActivity();
            bfActivity.Text = message;
            string jsonConnectorActivity = JsonConvert.SerializeObject(bfActivity);

            return await this.SendActivity(jsonConnectorActivity).ConfigureAwait(false);
        }

        /// <summary>
        /// Read audio wavFile.
        /// </summary>
        /// <param name="wavFile">WAV File in each turn.</param>
        public void ReadAudio(string wavFile)
        {
            lock (this.BotReplyList)
            {
                this.BotReplyList.Clear();
                this.indexActivityWithAudio = 0;
            }

            if (this.appsettings.PushStreamEnabled)
            {
                int readBytes;

                byte[] dataBuffer = new byte[MaxSizeOfTtsAudioInBytes];
                WaveFileReader waveFileReader = new WaveFileReader(Path.Combine(this.appsettings.InputFolder, wavFile));

                // Reading header bytes
                int headerBytes = waveFileReader.Read(dataBuffer, 0, WavHeaderSizeInBytes);

                while ((readBytes = waveFileReader.Read(dataBuffer, 0, BytesToRead)) > 0)
                {
                    this.pushAudioInputStream.Write(dataBuffer, readBytes);
                }

                // When done, we forcibly write one second (32000 bytes) of silence
                // to the stream, forcing the speech recognition service to segment.
                Array.Clear(dataBuffer, 0, 32000);
                this.pushAudioInputStream.Write(dataBuffer, 32000);

                waveFileReader.Dispose();
            }
            else
            {
                GlobalPullStream.FilePullStreamCallback.ReadFile(Path.Combine(this.appsettings.InputFolder, wavFile));
            }
        }

        /// <summary>
        /// Send an audio WAV file to the Bot using StartKeywordRecognitionAsync or ListenOnceAsync.
        /// </summary>
        /// <param name="wavFile">WAV file in each turn.</param>
        public void SendAudio(string wavFile)
        {
            this.ReadAudio(wavFile);

            if (!this.keyword)
            {
                Trace.TraceInformation($"[{DateTime.Now.ToString("hh:mm:ss.fff", CultureInfo.CurrentCulture)}] Start listening");

                // Don't wait for this task to finish. It may take a while, even after the "Recognized" event is received. This is a known
                // issue in Speech SDK and should be fixed in a future versions.
                this.connector.ListenOnceAsync();
            }
        }

        /// <summary>
        /// Sends an Activity to the Bot using SendActivityAsync.
        /// </summary>
        /// <param name="activity">Activity in each turn.</param>
        /// <returns>Activity.</returns>
        public async Task<BotConnector> SendActivity(string activity)
        {
            this.stopWatch.Restart();
            this.speechDuration = 0;

            lock (this.BotReplyList)
            {
                this.BotReplyList.Clear();
                this.indexActivityWithAudio = 0;
            }

            string result = await this.connector.SendActivityAsync(activity).ConfigureAwait(false);
            Trace.TraceInformation($"[{DateTime.Now.ToString("hh:mm:ss.fff", CultureInfo.CurrentCulture)}] Activity sent to channel. InteractionID {result}");
            return this;
        }

        /// <summary>
        /// Collects the expected number of bot reply activities and sorts them by timestamp.
        /// Filters the received activities and removes activities that are specified in the ignoreActivitiesList.
        /// The expected number of responses is set in the responseCount variable.
        /// </summary>
        /// <param name="bootstrapMode">Boolean which defines if turn is in bootstrapping mode or not.</param>
        /// <returns>List of time-sorted and filtered bot-reply Activities.</returns>
        public List<BotReply> WaitAndProcessBotReplies(bool bootstrapMode)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            List<BotReply> filteredBotReplyList = new List<BotReply>();
            int filteredactivities = 0;
            int botReplyIndex = 0;

            var getExpectedResponses = Task.Run(
                () =>
                {
                    // Make this configurable per interaction as an input row
                    while (bootstrapMode || filteredactivities < this.responseCount)
                    {
                        Thread.Sleep((int)ResponseCheckInterval);

                        lock (this.BotReplyList)
                        {
                            if (this.BotReplyList.Count != 0 && botReplyIndex < this.BotReplyList.Count)
                            {
                                if (this.IgnoreActivity(this.BotReplyList[botReplyIndex].Activity))
                                {
                                    this.BotReplyList[botReplyIndex].Ignore = true;
                                }
                                else
                                {
                                    filteredactivities++;
                                }

                                botReplyIndex++;
                            }
                        }
                    }

                    // Wait until TTS audio finishes downloading (if there is one), so its duration can be calculated. TTS audio duration
                    // may be part of test pass/fail validation.
                    while (this.ttsStreamDownloadCount > 0)
                    {
                        Thread.Sleep((int)ResponseCheckInterval);
                    }
                }, cancellationToken: token);

            if (Task.WhenAny(getExpectedResponses, Task.Delay((int)this.timeout)).Result == getExpectedResponses)
            {
                Trace.TraceInformation($"Task status {getExpectedResponses.Status}. Received {filteredactivities} activities, as expected (configured to wait for {this.responseCount}):");
            }
            else if (!bootstrapMode)
            {
                Trace.TraceInformation($"[{DateTime.Now.ToString("hh:mm:ss.fff", CultureInfo.CurrentCulture)}] Timed out waiting for expected replies. Received {filteredactivities} activities (configured to wait for {this.responseCount}):");
                source.Cancel();
            }
            else
            {
                Trace.TraceInformation($"[{DateTime.Now.ToString("hh:mm:ss.fff", CultureInfo.CurrentCulture)}] Received {filteredactivities} activities.");
                source.Cancel();
            }

            for (int filteredBotReplyIndex = 0; filteredBotReplyIndex < this.BotReplyList.Count && filteredBotReplyList.Count < filteredactivities; filteredBotReplyIndex++)
            {
                if (this.BotReplyList[filteredBotReplyIndex].Ignore == false)
                {
                    filteredBotReplyList.Add(this.BotReplyList[filteredBotReplyIndex]);
                }
            }

            for (int count = 0; count < filteredBotReplyList.Count; count++)
            {
                Trace.TraceInformation($"[{count}]: Latency {filteredBotReplyList[count].Latency} msec");
            }

            filteredBotReplyList.Sort((a, b) =>
            {
                return DateTimeOffset.Compare(a.Activity.Timestamp ?? default, b.Activity.Timestamp ?? default);
            });

            source.Dispose();
            return filteredBotReplyList;
        }

        /// <summary>
        /// Disposes the DialogServiceConnector object.
        /// </summary>
        public void Dispose()
        {
            this.kwsTable?.Dispose();
            this.connector.Dispose();
            this.audioConfig.Dispose();
            if (this.appsettings.PushStreamEnabled)
            {
                this.pushAudioInputStream.Dispose();
            }
        }

        /// <summary>
        /// Compares a given activity to the list of activities specified by IgnoringActivities list in the test configuration.
        /// </summary>
        /// <param name="activity">An activity that the client received from the bot.</param>
        /// <returns>true if the activity matches one of the activities in the list. Otherwise returns false.</returns>
        public bool IgnoreActivity(Activity activity)
        {
            bool ignore = false;

            if (this.ignoreActivitiesList != null)
            {
                foreach (Activity activityToIgnore in this.ignoreActivitiesList)
                {
                    string activityToIgnoreSerializedJson = JsonConvert.SerializeObject(activityToIgnore, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    string activitySerializedJson = JsonConvert.SerializeObject(activity, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    JObject activityToIgnoreJObject = JsonConvert.DeserializeObject<JObject>(activityToIgnoreSerializedJson);
                    JObject activityJObject = JsonConvert.DeserializeObject<JObject>(activitySerializedJson);

                    DialogResult.ActivityMismatchCount = 0;
                    if (DialogResult.CompareJObjects(activityToIgnoreJObject, activityJObject) == 0)
                    {
                        Trace.TraceInformation($"Bot-reply activity matched IgnoreActivities[{this.ignoreActivitiesList.IndexOf(activityToIgnore)}]. Ignore it.");
                        ignore = true;
                        break;
                    }
                }
            }

            return ignore;
        }

        /// <summary>
        /// Obtains and sets the utterance, dialogId, and turnId, and responseCount for each Dialog and Turn
        /// Obtains ad sets list of ignoringActivities and timeout value defined in the Config for each InputFile.
        /// </summary>
        /// <param name="fileName">The name of the input test file.</param>
        /// <param name="dialogID">The value of the DialogID in the input test file.</param>
        /// <param name="turnID">The value of the TurnID in the input test file.</param>
        /// <param name="responseCount">Number of bot activity responses expected for this turn (after filtering out activities marked for ignoring).</param>
        /// <param name="ignoringActivities">List of Activities to Ignore for the bot as defined in the Config File.</param>
        /// <param name="keyword">Bool value of keyword for each turn.</param>
        public void SetInputValues(string fileName, string dialogID, int turnID, int responseCount, List<Activity> ignoringActivities, bool keyword)
        {
            this.baseFileName = Path.GetFileNameWithoutExtension(fileName);
            this.dialogID = dialogID;
            this.turnID = turnID;
            this.responseCount = responseCount;
            this.ignoreActivitiesList = ignoringActivities;
            this.keyword = keyword;
        }

        /// <summary>
        /// Write header to a WAV file.
        /// </summary>
        /// <param name="fs"> Filestream.</param>
        private static void WriteWavHeader(FileStream fs)
        {
            ushort channels = 1;
            int sampleRate = 16000;
            ushort bytesPerSample = 2;

            fs.Position = 0;

            // RIFF header.
            // Chunk ID.
            fs.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);

            // Chunk size.
            fs.Write(BitConverter.GetBytes((int)fs.Length - 8), 0, 4);

            // Format.
            fs.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);

            // Sub-chunk 1.
            // Sub-chunk 1 ID.
            fs.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);

            // Sub-chunk 1 size.
            fs.Write(BitConverter.GetBytes(16), 0, 4);

            // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            fs.Write(BitConverter.GetBytes((ushort)1), 0, 2);

            // Channels.
            fs.Write(BitConverter.GetBytes(channels), 0, 2);

            // Sample rate.
            fs.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // Bytes rate.
            fs.Write(BitConverter.GetBytes(sampleRate * channels * bytesPerSample), 0, 4);

            // Block align.
            fs.Write(BitConverter.GetBytes((ushort)channels * bytesPerSample), 0, 2);

            // Bits per sample.
            fs.Write(BitConverter.GetBytes((ushort)(bytesPerSample * 8)), 0, 2);

            // Sub-chunk 2.
            // Sub-chunk 2 ID.
            fs.Write(Encoding.ASCII.GetBytes("data"), 0, 4);

            // Sub-chunk 2 size.
            fs.Write(BitConverter.GetBytes((int)(fs.Length - 44)), 0, 4);
        }

        private void AttachHandlers()
        {
            if (this.connector != null)
            {
                this.connector.ActivityReceived += this.SpeechBotConnector_ActivityReceived;
                this.connector.Recognized += this.SpeechBotConnector_Recognized;
                this.connector.Recognizing += this.SpeechBotConnector_Recognizing;
                this.connector.Canceled += this.SpeechBotConnector_Canceled;
                this.connector.SessionStarted += this.SpeechBotConnector_SessionStarted;
                this.connector.SessionStopped += this.SpeechBotConnector_SessionStopped;
            }
        }

        private void SpeechBotConnector_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizingKeyword)
            {
                Trace.TraceInformation($"Keyword Recognition: Verifying: {e.Result.Text}");
            }
        }

        private void DetachHandlers()
        {
            if (this.connector != null)
            {
                this.connector.ActivityReceived -= this.SpeechBotConnector_ActivityReceived;
                this.connector.Canceled -= this.SpeechBotConnector_Canceled;
            }
        }

        private void SpeechBotConnector_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                this.RecognizedText = e.Result.Text;
                this.speechDuration = (int)e.Result.Duration.TotalMilliseconds;

                Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Recognized event received. SessionId = {e.SessionId}, Speech duration = {this.speechDuration}, Recognized text = {this.RecognizedText}");
            }
            else if (e.Result.Reason == ResultReason.RecognizedKeyword)
            {
                this.RecognizedKeyword = e.Result.Text;

                Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Recognized event received. SessionId = {e.SessionId}");
                Trace.TraceInformation($"Keyword Recognition Verified : {e.Result.Text}");
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Recognized event received. Speech could not be recognized. SessionId = {e.SessionId}");
                Trace.TraceInformation($"No match details = {NoMatchDetails.FromResult(e.Result)}");
            }
            else
            {
                Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Recognized event received. e.Result.Reason = {e.Result.Reason}. SessionId = {e.SessionId}");
            }
        }

        private void SpeechBotConnector_ActivityReceived(object sender, ActivityReceivedEventArgs e)
        {
            var json = e.Activity;
            var activity = JsonConvert.DeserializeObject<Activity>(json);

            // TODO: When there is TTS audio, get the elapsed time only after first TTS buffer was received
            int elapsedTime = (int)this.stopWatch.ElapsedMilliseconds;

            if (this.appsettings.RealTimeAudio)
            {
                // For WAV file input, the timer starts on SessionStart event. If we consume the audio from the input stream at real-time, then by subtracting
                // the speech duration here, its as if we started the timer at the point that speech stopped. This is what we want to accurately measure UPL.
                // For any other input (text or Activity), the speechDuration value is not relevant and should be zero at this point.
                elapsedTime -= this.speechDuration;
            }

            Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Activity received, elapsedTime = {elapsedTime}, speechDuration = {this.speechDuration}");

            int activityIndex = 0;
            int ttsDuration = 0;

            lock (this.BotReplyList)
            {
                this.BotReplyList.Add(new BotReply(activity, elapsedTime, false));
                activityIndex = this.BotReplyList.Count - 1;
            }

            if (e.HasAudio)
            {
                this.ttsStreamDownloadCount++;
                this.indexActivityWithAudio++;
                ttsDuration = this.WriteAudioToWAVfile(e.Audio, this.baseFileName, this.dialogID, this.turnID, this.indexActivityWithAudio - 1);
                this.ttsStreamDownloadCount--;
                lock (this.BotReplyList)
                {
                    this.BotReplyList[activityIndex].TTSAudioDuration = ttsDuration;
                }
            }
        }

        private void SpeechBotConnector_Canceled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            if (e.Reason == CancellationReason.Error)
            {
                Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Canceled event received due to an error. {e.ErrorCode} - {e.ErrorDetails}.");
            }
            else if (e.Reason == CancellationReason.EndOfStream)
            {
                Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Canceled event received due to end of stream.");
            }
        }

        private void SpeechBotConnector_SessionStarted(object sender, SessionEventArgs e)
        {
            this.stopWatch.Restart();
            Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Session Started event received. SessionId = {e.SessionId}");
        }

        private void SpeechBotConnector_SessionStopped(object sender, SessionEventArgs e)
        {
            Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Session Stopped event received. SessionId = {e.SessionId}");
        }

        /// <summary>
        /// Write TTS Audio to WAV file.
        /// </summary>
        /// <param name="audio"> TTS Audio.</param>
        /// <param name="baseFileName"> File name where this test is specified. </param>
        /// <param name="dialogID">The value of the DialogID in the input test file.</param>
        /// <param name="turnID">The value of the TurnID in the input test file.</param>
        /// <param name="indexActivityWithAudio">Index value of the current TTS response.</param>
        private int WriteAudioToWAVfile(PullAudioOutputStream audio, string baseFileName, string dialogID, int turnID, int indexActivityWithAudio)
        {
            FileStream fs = null;
            string testFileOutputFolder = Path.Combine(this.appsettings.OutputFolder, baseFileName);
            string wAVFolderPath = Path.Combine(testFileOutputFolder, ProgramConstants.WAVFileFolderName);
            int durationInMS = 0;

            if (indexActivityWithAudio == 0)
            {
                // First TTS WAV file to be written, create the WAV File Folder
                Directory.CreateDirectory(wAVFolderPath);
            }

            this.outputWAV = Path.Combine(wAVFolderPath, baseFileName + "-BotResponse-" + dialogID + "-" + turnID + "-" + indexActivityWithAudio + ".WAV");
            byte[] buff = new byte[MaxSizeOfTtsAudioInBytes];
            uint bytesReadtofile;

            try
            {
                fs = File.Create(this.outputWAV);
                fs.Write(new byte[WavHeaderSizeInBytes]);
                while ((bytesReadtofile = audio.Read(buff)) > 0)
                {
                    fs.Write(buff, 0, (int)bytesReadtofile);
                }

                WriteWavHeader(fs);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
            finally
            {
                fs.Close();
            }

            WaveFileReader waveFileReader = new WaveFileReader(this.outputWAV);
            durationInMS = (int)waveFileReader.TotalTime.TotalMilliseconds;
            waveFileReader.Dispose();
            return durationInMS;
        }
    }
}