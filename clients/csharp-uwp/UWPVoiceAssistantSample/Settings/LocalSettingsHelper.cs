// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using UWPVoiceAssistantSample.AudioCommon;
    using Windows.ApplicationModel;
    using Windows.Storage;
    using Windows.UI.Xaml;

    /// <summary>
    /// A convenience wrapper for getting and setting well-known properties from AppLocal settings.
    /// </summary>
    public static class LocalSettingsHelper
    {
        private const string LocalConfigFilename = "config.json";
        private static readonly ILogProvider Log = LogRouter.GetClassLogger();
        private static readonly StorageFolder LocalConfigFolder = ApplicationData.Current.LocalFolder;
        private static readonly Uri DefaultConfigUri = new Uri($"ms-appx:///Assets/defaultConfig.json");
        private static DialogAudio cachedOutputFormat;

        /// <summary>
        /// Gets or sets a value indicating whether the Speech SDK (Direct Line Speech) should be
        /// used as the selected dialog backend.
        /// </summary>
        public static bool EnableSpeechSDK
        {
            get => ReadValueWithDefault<bool>("enableSpeechSDK", true);
            set => WriteValue("enableSpeechSDK", value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether 2nd-stage keyword spotting is required before
        /// transitioning from background to foreground and taking action.
        /// </summary>
        public static bool EnableSecondStageKws
        {
            get => ReadValueWithDefault<bool>("enableSecondStageKws", true);
            set => WriteValue("enableSecondStageKws", value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the chosen dialog backend should emit a log
        /// file to application local state for diagnostic purposes.
        /// </summary>
        public static bool EnableSdkLogging
        {
            get => ReadValueWithDefault<bool>("enableSdkLogging", false);
            set => WriteValue("enableSdkLogging", value);
        }

        /// <summary>
        /// Gets or sets the subscription key used by Direct Line Speech for dialog.
        /// </summary>
        public static string SpeechSubscriptionKey
        {
            get => ReadValueWithDefault<string>("DialogServiceConnector_speechSubscriptionKey", string.Empty);
            set => WriteValue("DialogServiceConnector_speechSubscriptionKey", value);
        }

        /// <summary>
        /// Gets or sets the Azure service region for the selected speech subscription.
        /// </summary>
        public static string AzureRegion
        {
            get => ReadValueWithDefault<string>("DialogServiceConnector_azureRegion", string.Empty);
            set => WriteValue("DialogServiceConnector_azureRegion", value);
        }

        /// <summary>
        /// Gets or sets the optional custom speech endpoint ID as provided through Speech Studio.
        /// </summary>
        public static string CustomSpeechId
        {
            get => ReadValueWithDefault<string>("DialogServiceConnector_customSREndpointID", string.Empty);
            set => WriteValue("DialogServiceConnector_customSREndpointID", value);
        }

        /// <summary>
        /// Gets or sets the optional collection of comma-separated custom voice IDs as provided
        /// through Speech Studio.
        /// </summary>
        public static string CustomVoiceIds
        {
            get => ReadValueWithDefault<string>("DialogServiceConnector_customVoiceID", string.Empty);
            set => WriteValue("DialogServiceConnector_customVoiceID", value);
        }

        /// <summary>
        /// Gets or sets the optional custom commands app id provided through Speech Studio.
        /// </summary>
        public static string CustomCommandsAppId
        {
            get => ReadValueWithDefault<string>("DialogServiceConnector_customCommandsAppID", string.Empty);
            set => WriteValue("DialogServiceConnector_customCommandsAppID", value);
        }

        /// <summary>
        /// Gets or sets the bot id associated for the selected speech subscription.
        /// </summary>
        public static string BotId
        {
            get => ReadValueWithDefault<string>("DialogServiceConnector_botID", string.Empty);
            set => WriteValue("DialogServiceConnector_botID", value);
        }

        /// <summary>
        /// Gets or sets the KeywordDisplayName.
        /// </summary>
        public static string KeywordDisplayName
        {
            get => ReadValueWithDefault<string>("keywordDisplayName", string.Empty);
            set => WriteValue("keywordDisplayName", value);
        }

        /// <summary>
        /// Gets or sets the KeywordId.
        /// </summary>
        public static string KeywordId
        {
            get => ReadValueWithDefault<string>("keywordID", string.Empty);
            set => WriteValue("keywordID", value);
        }

        /// <summary>
        /// Gets or sets the KeywordModelId.
        /// </summary>
        public static string KeywordModelId
        {
            get => ReadValueWithDefault<string>("keywordModelId", string.Empty);
            set => WriteValue("keywordModelId", value);
        }

        /// <summary>
        /// Gets or sets the KeywordActivationModelDataFormat.
        /// </summary>
        public static string KeywordActivationModelDataFormat
        {
            get => ReadValueWithDefault<string>("keywordActivationModelDataFormat", string.Empty);
            set => WriteValue("keywordActivationModelDataFormat", value);
        }

        /// <summary>
        /// Gets or sets the KeywordActivationModelPath.
        /// </summary>
        public static string KeywordActivationModelPath
        {
            get => ReadValueWithDefault<string>("keywordActivationModelPath", string.Empty);
            set => WriteValue("keywordActivationModelPath", value);
        }

        /// <summary>
        /// Gets or sets the KeywordConfirmationModelPath.
        /// </summary>
        public static string KeywordConfirmationModelPath
        {
            get => ReadValueWithDefault<string>("keywordConfirmationModelPath", string.Empty);
            set => WriteValue("keywordConfirmationModelPath", value);
        }

        /// <summary>
        /// Gets or sets the KwsPerformanceLogging feature.
        /// </summary>
        public static bool KwsPerfomanceLogging
        {
            get => ReadValueWithDefault<bool>("kwsPerformanceLogging", false);
            set => WriteValue("kwsPerformanceLogging", value);
        }

        public static DialogAudio OutputFormat
        {
            get
            {
                if (cachedOutputFormat == null)
                {
                    var serialized = ReadValueWithDefault<ApplicationDataCompositeValue>("AudioOutputFormat", null);
                    if (serialized != null
                        && DialogAudio.TryGetFromSettingsValue(serialized, out DialogAudio audioFromSettings))
                    {
                        cachedOutputFormat = audioFromSettings;
                    }
                    else
                    {
                        cachedOutputFormat = DirectLineSpeechAudio.DefaultOutput;
                    }
                }

                return cachedOutputFormat;
            }

            set
            {
                if (value != null)
                {
                    WriteValue("AudioOutputFormat", value.SerializeToSettingsValue());
                    cachedOutputFormat = value;
                    Log.Log(LogMessageLevel.Noise, $"AudioOutputFormat updated to {value.Label}");
                }
                else if (value == null && ApplicationData.Current.LocalSettings.Values.ContainsKey("AudioOutputFormat"))
                {
                    ApplicationData.Current.LocalSettings.Values.Remove("AudioOutputFormat");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether audio capture should emit files recording
        /// all incoming audio, including 1st-stage activations. Used for diagnostic purposes only.
        /// </summary>
        public static bool EnableAudioCaptureFiles
        {
            get => ReadValueWithDefault<bool>("enableAudioCaptureFiles", false);
            set => WriteValue("enableAudioCaptureFiles", value);
        }

        /// <summary>
        /// Loads file-backed initial values for settings, ensuring that a configuration file exists in a writeable
        /// location if it doesn't already.
        /// </summary>
        /// <returns> A task that completes once file-based settings are initialized. </returns>
        public static async Task InitializeAsync()
        {
            await EnsureFilesInLocalFolderAsync();
            var configFile = await LocalConfigFolder.GetFileAsync(LocalConfigFilename);

            if (!string.IsNullOrWhiteSpace(configFile.Path))
            {
                AppSettings appSettings = await AppSettings.Load(configFile);

                SpeechSubscriptionKey = appSettings.SpeechSubscriptionKey;
                AzureRegion = appSettings.AzureRegion;
                CustomSpeechId = appSettings.CustomSpeechId;
                CustomVoiceIds = appSettings.CustomVoiceIds;
                CustomCommandsAppId = appSettings.CustomCommandsAppId;
                BotId = appSettings.BotId;
                KeywordDisplayName = appSettings.KeywordActivationModel.DisplayName;
                KeywordActivationModelPath = appSettings.KeywordActivationModel.Path;
                KeywordId = appSettings.KeywordActivationModel.KeywordId;
                KeywordModelId = appSettings.KeywordActivationModel.ModelId;
                KeywordActivationModelDataFormat = appSettings.KeywordActivationModel.ModelDataFormat;
                KeywordConfirmationModelPath = appSettings.KeywordModel;
                KwsPerfomanceLogging = appSettings.KwsPerfomanceLogging;
            }
        }

        /// <summary>
        /// Writes a provided object to app-local settings via ApplicationData APIs.
        /// </summary>
        /// <param name="key"> The key under which the setting is to be stored. </param>
        /// <param name="newValue"> The object (string-serializable) to be recorded. </param>
        public static void WriteValue(string key, object newValue)
        {
            ApplicationData.Current.LocalSettings.Values[key] = newValue;
        }

        /// <summary>
        /// Reads the object associated with the provided key, if present, and casts it to the
        /// templated type. Returns the provided default value if no value currently exists that
        /// is associated with the key.
        /// </summary>
        /// <typeparam name="T"> The expected type of the setting value stored. </typeparam>
        /// <param name="key"> The key under which the setting is stored. </param>
        /// <param name="defaultValue">
        ///     The value to use if no settings-backed value for the provided key currently exists.
        /// </param>
        /// <returns> The value or default for the given key. </returns>
        public static T ReadValueWithDefault<T>(string key, T defaultValue)
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            T result;

            if (settings.ContainsKey(key))
            {
                result = (T)settings[key];
            }
            else
            {
                result = defaultValue;
            }

            return result;
        }

        private static async Task EnsureFilesInLocalFolderAsync()
        {
            var copiesToEnsure = new (StorageFile source, FileInfo destination)[]
            {
                (await StorageFile.GetFileFromApplicationUriAsync(DefaultConfigUri),
                    new FileInfo(Path.Combine(LocalConfigFolder.Path, LocalConfigFilename))),
            };

            copiesToEnsure.Where(copyToEnsure => !copyToEnsure.destination.Exists)
                .ToList()
                .ForEach(copyOperation => File.Copy(copyOperation.source.Path, copyOperation.destination.FullName));
        }
    }
}
