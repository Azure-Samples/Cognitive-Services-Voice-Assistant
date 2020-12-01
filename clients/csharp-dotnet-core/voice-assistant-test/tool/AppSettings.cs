// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VoiceAssistantTest.Resources;

    /// <summary>
    /// Application settings defined for the Bot.
    /// </summary>
    internal class AppSettings
    {
        /// <summary>
        /// Gets or sets test configuration of each Input File.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public TestSettings[] Tests { get; set; }

        /// <summary>
        /// Gets or sets Speech Subscription Key.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SpeechSubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets Speech Subscription Region.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SpeechRegion { get; set; }

        /// <summary>
        /// Gets or sets the margin for the duration of the Text-To-Speech audio sent by the bot, if any.
        /// The margin sets the acceptable delta value (in excess, or short of) for the actual TTS audio duration (in milliseconds).
        /// </summary>
        public int TTSAudioDurationMargin { get; set; } = ProgramConstants.DefaultTTSAudioDurationMargin;

        /// <summary>
        /// Gets or sets a value indicating whether to log application traces to a text file.
        /// The text file name is VoiceAssistantTest.log and it will be written to the folder
        /// specified by the OutputFolder property.
        /// </summary>
        public bool AppLogEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether a Speech SDK log should be written when the test is run.
        /// Beware, this log is very verbose. Turn on only for a small number of
        /// test runs when you want to report a Speech SDK bug to Microsoft.
        /// The file name will have the time-stamped format SpeechSDKLog-yyyy-MM-dd-HH-mm-ss.log,
        /// and it will be written to the folder specified by the OutputFolder property.
        /// </summary>
        public bool SpeechSDKLogEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the speech recognition language.
        /// </summary>
        public string SRLanguage { get; set; } = "en-us";

        /// <summary>
        /// Gets or sets the Custom Commands Application ID. If this value is not set,
        /// the default is to connect to a Bot-Framework bot registered with Direct
        /// Line Speech channel.
        /// </summary>
        public string CustomCommandsAppId { get; set; }

        /// <summary>
        /// Gets or sets the Custom Speech Recognition endpoint ID, as provided
        /// by the Speech Studio Custom Speech web portal. If this value is not
        /// set, use standard Cognitive Services Speech Recognition endpoint without
        /// customization.
        /// </summary>
        public string CustomSREndpointId { get; set; }

        /// <summary>
        /// Gets or sets the Custom Voice Deployment IDs, as provided
        /// by the Speech Studio Custom Voice web portal. If this value is not
        /// set, the bot can only use one of the ready-made Text-To-Speech voices.
        /// </summary>
        public string CustomVoiceDeploymentIds { get; set; }

        /// <summary>
        /// Gets or sets the Aria Project Key (also known as Ingestion Key).
        /// If supplied, the tool will send events to your Aria project
        /// for dialog on test pass success/failure.
        /// See https://www.aria.ms/.
        /// </summary>
        public string AriaProjectKey { get; set; }

        /// <summary>
        /// Gets or sets the root input folder. This root folder will be added
        /// to all file names listed in the InputFiles array.
        /// </summary>
        public string InputFolder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the output folder. Test result and application
        /// log file will be written to this folder.
        /// </summary>
        public string OutputFolder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether a Bot has a Greeting.
        /// </summary>
        public bool BotGreeting { get; set; } = false;

        /// <summary>
        /// Gets or sets the timeout value. If this value is not set, the default is 5000 milliseconds.
        /// </summary>
        public int Timeout { get; set; } = ProgramConstants.DefaultTimeout;

        /// <summary>
        /// Gets or sets the Keyword Recognition Model File.
        /// </summary>
        public string KeywordRecognitionModel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use a push stream instead of a pull stream.
        /// </summary>
        public bool PushStreamEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not keyword verification is perfomred by the speech service.
        /// </summary>
        public bool KeywordVerificationEnabled { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the SetPropertyID.
        /// This will result in SetPropery method call on DialogServiceConfig that takes a PropertyId argument.
        /// </summary>
        public JObject SetPropertyId { get; set; }

        /// <summary>
        /// Gets or sets the SetPropertyString.
        /// This will result in SetPropery method call on DialogServiceConfig that takes a string argument.
        /// </summary>
        public JObject SetPropertyString { get; set; }

        /// <summary>
        /// Gets or sets the SetServiceProperty.
        /// This will result in SetServiceProperty method call on DialogServiceConfig.
        /// </summary>
        public JObject SetServiceProperty { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to send audio in real time through PushAudioInputStream.
        /// </summary>
        public bool RealTimeAudio { get; set; }

        /// <summary>
        /// Obtain values from AppSettings.json and populate an instance with associated values.
        /// </summary>
        /// <param name="configFile">Input JSON configuration file.</param>
        /// <returns>An AppSettings instance.</returns>
        public static AppSettings Load(string configFile)
        {
            Trace.TraceInformation($"Parsing {configFile}");

            StreamReader file = new StreamReader(configFile);
            string config = file.ReadToEnd();
            file.Close();
            AppSettings instance = JsonConvert.DeserializeObject<AppSettings>(config);
            ValidateAppSettings(instance);

            return instance;
        }

        /// <summary>
        /// Validate Subscription Key by parsing to a GUID.
        /// </summary>
        /// <param name="key">Input Subscription Key.</param>
        /// <returns>Boolean - true for a valid subscription key, false otherwise.</returns>
        public static bool ValidateSubscriptionKey(string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
            {
                throw new MissingFieldException(ErrorStrings.SUBSCRIPTION_KEY_MISSING);
            }

            if (Guid.TryParse(key, out Guid parsedGuid) &&
                parsedGuid.ToString("N", null).Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates CustomSREndpointId/ CustomVoiceDeploymentIds if it is a GUID and case insensitive.
        /// </summary>
        /// <param name="id">Input id.</param>
        /// <returns>Boolean - true for a valid CustomCommandsAppId /CustomSREndpointId/ CustomVoiceDeploymentIds, false otherwise.</returns>
        public static bool ValidateCustomID(string id)
        {
            if (Guid.TryParse(id, out Guid parsedGuid))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates CustomCommandsAppId if it isa GUID with dashes and case sensitive.
        /// </summary>
        /// <param name="id">Input id.</param>
        /// <returns>Boolean - true for a valid CustomCommandsAppId, false otherwise.</returns>
        public static bool ValidateCustomCommandAppID(string id)
        {
            if (Guid.TryParse(id, out Guid parsedGuid) &&
                parsedGuid.ToString("D", null).Equals(id, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///  Looking up for region from the list.
        /// </summary>
        /// <param name="region">Speech region.</param>
        /// <returns>Boolean - true for a valid Direct Line Speech Azure Region false otherwise.</returns>
        public static bool ValidateRegion(string region)
        {
            List<string> speechRegions = new List<string>() { "westus", "westus2", "eastus", "eastus2", "westeurope", "northeurope", "southeastasia" };

            return speechRegions.Contains(region.ToLower(CultureInfo.CurrentCulture));
        }

        /// <summary>
        ///  Looking up for Speech Recognition Language from the list.
        /// </summary>
        /// <param name="srLanguage">Speech Recognition Language.</param>
        /// <returns>Boolean - true for a valid Speech Recognition Language, false otherwise.</returns>
        public static bool ValidateSRLanguage(string srLanguage)
        {
            List<string> srLanguages = new List<string>() { "ar-eg", "ar-sa", "ar-ae", "ar-kw", "ar-qa", "ca-es", "da-dk", "de-de", "en-au", "en-ca", "en-gb", "en-in", "en-nz", "en-us", "es-es", "es-mx", "fi-fi", "fr-ca", "fr-fr", "gu-in", "hi-in", "it-it", "ja-jp", "ko-kr", "mr-in", "nb-no", "nl-nl", "pl-pl", "pt-br", "pt-pt", "ru-ru", "sv-se", "ta-in", "te-in", "zh-cn", "zh-hk", "zh-tw" };

            return srLanguages.Contains(srLanguage.ToLower(CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Validate an AppSettings instance.
        /// </summary>
        /// <param name="instance">An instance of AppSettings.</param>
        public static void ValidateAppSettings(AppSettings instance)
        {
            if (ValidateSubscriptionKey(instance.SpeechSubscriptionKey) == false)
            {
                throw new ArgumentException(ErrorStrings.SUBSCRIPTION_KEY_INVALID);
            }

            if (string.IsNullOrWhiteSpace(instance.SpeechRegion) && instance.SetPropertyString == null)
            {
                throw new MissingFieldException(ErrorStrings.AZURE_REGION_MISSING);
            }
            else if (!string.IsNullOrWhiteSpace(instance.SpeechRegion) && ValidateRegion(instance.SpeechRegion) == false)
            {
                throw new ArgumentException(ErrorStrings.AZURE_REGION_INVALID);
            }

            if (instance.Tests == null || instance.Tests.Length == 0)
            {
                throw new MissingFieldException(ErrorStrings.INPUT_FILE_MISSING);
            }

            string inputDirectory = instance.InputFolder;

            if (string.IsNullOrEmpty(instance.InputFolder))
            {
                inputDirectory = Directory.GetCurrentDirectory();
            }

            string outputDirectory = string.Empty;

            if (!string.IsNullOrWhiteSpace(instance.OutputFolder))
            {
                outputDirectory = Path.GetFullPath(instance.OutputFolder);
                if (!Directory.Exists(outputDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }
                    catch (Exception)
                    {
                        throw new Exception($"{ErrorStrings.FAILED_CREATING_OUPUT_FOLDER} - {outputDirectory}");
                    }
                }
            }

            if (string.IsNullOrEmpty(instance.OutputFolder))
            {
                outputDirectory = Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(outputDirectory))
            {
                throw new DirectoryNotFoundException($"{ErrorStrings.FOLDER_NOT_FOUND} - {outputDirectory}");
            }

            if (!HasWriteAccessToDirectory(outputDirectory))
            {
                // If output folder is set, check if it has write permissions
                throw new UnauthorizedAccessException($"{ErrorStrings.NO_WRITE_ACCESS_FOLDER} - {outputDirectory}");
            }

            if (!string.IsNullOrWhiteSpace(instance.CustomCommandsAppId))
            {
                if (ValidateCustomCommandAppID(instance.CustomCommandsAppId) == false)
                {
                    throw new ArgumentException(ErrorStrings.CUSTOM_COMMANDS_APP_ID_INVALID);
                }
            }

            if (!string.IsNullOrWhiteSpace(instance.CustomSREndpointId))
            {
                if (ValidateCustomID(instance.CustomSREndpointId) == false)
                {
                    throw new ArgumentException(ErrorStrings.CUSTOM_SR_ENDPOINT_ID_INVALID);
                }
            }

            if (!string.IsNullOrWhiteSpace(instance.CustomVoiceDeploymentIds))
            {
                if (ValidateCustomID(instance.CustomVoiceDeploymentIds) == false)
                {
                    throw new ArgumentException(ErrorStrings.CUSTOM_VOICE_DEPLOYMENT_ID_INVALID);
                }
            }

            if (!string.IsNullOrWhiteSpace(instance.SRLanguage))
            {
                if (ValidateSRLanguage(instance.SRLanguage) == false)
                {
                    throw new ArgumentException(ErrorStrings.SR_LANGUAGE_INVALID);
                }
            }

            string keywordFile = string.Empty;

            if (!string.IsNullOrWhiteSpace(instance.KeywordRecognitionModel))
            {
                keywordFile = Path.GetFullPath(instance.KeywordRecognitionModel);
                if (!File.Exists(keywordFile))
                {
                    throw new MissingFieldException($"{ErrorStrings.KEYWORD_RECOGNITION_MODEL_NOT_FOUND} - {keywordFile}");
                }
            }

            if (instance.TTSAudioDurationMargin < 0)
            {
                throw new MissingFieldException($"{ErrorStrings.TTS_AUDIO_DURATION_MARGIN_INVALID} - {keywordFile}");
            }

            if (instance.Timeout < 0)
            {
                throw new MissingFieldException($"{ErrorStrings.TIMEOUT_INVALID} - {keywordFile}");
            }
        }

        /// <summary>
        /// Check if the given directory has write access.
        /// </summary>
        /// <param name="directory">A string representing the full path of the directory.</param>
        /// <returns>True if the directory has write access, false otherwise.</returns>
        private static bool HasWriteAccessToDirectory(string directory)
        {
            bool hasWriteAccess = false;
            string fullPath = Path.Combine(directory, ProgramConstants.TestReportFileName);

            try
            {
                File.WriteAllText(fullPath, "\n");

                if (File.Exists(fullPath))
                {
                    hasWriteAccess = true;
                }
            }
            catch (Exception)
            {
                hasWriteAccess = false;
            }

            return hasWriteAccess;
        }
    }
}