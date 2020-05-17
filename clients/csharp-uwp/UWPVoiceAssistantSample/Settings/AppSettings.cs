// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Windows.Storage;

    /// <summary>
    /// Bot specific application settings obtained for config.json.
    /// </summary>
    public class AppSettings
    {
        private static readonly ILogProvider Logger = LogRouter.GetClassLogger();

        /// <summary>
        /// Gets or sets Speech Subscription Key.
        /// </summary>
        public string SpeechSubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets Speech Region.
        /// </summary>
        public string SpeechRegion { get; set; }

        /// <summary>
        /// Gets or sets SR Language.
        /// </summary>
        public string SRLanguage { get; set; }

        /// <summary>
        /// Gets or sets CustomSREndpointId.
        /// </summary>
        public string CustomSREndpointId { get; set; }

        /// <summary>
        /// Gets or sets CustomVoiceDeploymentIds.
        /// </summary>
        public string CustomVoiceDeploymentIds { get; set; }

        /// <summary>
        /// Gets or sets Custom Commands App Id.
        /// </summary>
        public string CustomCommandsAppId { get; set; }

        /// <summary>
        /// Gets or sets the URL override.
        /// </summary>
        public Uri UrlOverride { get; set; }

        /// <summary>
        /// Gets or sets SpeechSDKLogEnabled.
        /// </summary>
        public bool SpeechSDKLogEnabled { get; set; }

        /// <summary>
        /// Gets or sets Bot Id.
        /// </summary>
        public string BotId { get; set; }

        /// <summary>
        /// Gets or sets the details of the keyword activation model used for initial detection of a keyword.
        /// </summary>
        public KeywordActivationModel KeywordActivationModel { get; set; }

        /// <summary>
        /// Gets or sets the path of the keyword confirmation model used for confirming the inital detection of the
        /// activation model on the device.
        /// </summary>
        public string KeywordRecognitionModel { get; set; }

        /// <summary>
        /// Reads and deserializes the configuration file.
        /// </summary>
        /// <param name="configFile">config.json.</param>
        /// <returns>Instance of AppSettings.</returns>
        public static async Task<AppSettings> Load(StorageFile configFile)
        {
            using (var stream = await configFile.OpenStreamForReadAsync())
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                var settings = serializer.Deserialize<AppSettings>(jsonReader);
                LogInvalidSettings(settings);
                return settings;
            }
        }

        /// <summary>
        /// Verifies if Azure Region provided is listed within the speechRegions.
        /// </summary>
        /// <param name="region">Azure Region.</param>
        /// <returns>Bool - true if region is within the speechRegions.</returns>
        public static bool IsValidAzureRegion(string region)
            => new string[]
            {
                "westus",
                "westus2",
                "eastus",
                "eastus2",
                "westeurope",
                "northeurope",
                "southeastasia",
            }.Contains(region, StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        /// Verifies if a provided resource ID is either both optional and omitted or otherwise parses to a valid
        /// GUID.
        /// </summary>
        /// <param name="id">
        ///     A resource ID that may include a speech subscription key, custom speech id, custom voice id, or Custom Commands id.
        /// </param>
        /// <param name="optional"> Whether the provided resource is optional and valid if empty. </param>
        /// <returns> Whether the provided resource id is valid. </returns>
        public static bool IsValidResourceId(string id, bool optional)
            => (optional && string.IsNullOrEmpty(id)) || Guid.TryParse(id, out Guid _);

        /// <summary>
        /// Verfies keyword activation and confirmation paths are not null and start with ms-appx:///.
        /// </summary>
        /// <param name="path">Keyword activation and confirmation file paths, relative to ms-appx:///.</param>
        /// <returns>Bool - true if path is not null and starts with ms-appx:///.</returns>
        public static bool IsValidModelPath(string path)
            => !string.IsNullOrEmpty(path) && (path.StartsWith("ms-app", StringComparison.CurrentCultureIgnoreCase) || File.Exists(path));

        /// <summary>
        /// Validate AppSettings instance.
        /// </summary>
        /// <param name="instance">An instance of AppSettings.</param>
        public static void LogInvalidSettings(AppSettings instance)
        {
            Contract.Requires(instance != null);

            void LogIfFalse(bool condition, string message)
            {
                if (!condition)
                {
                    Logger.Log(message);
                }
            }

            LogIfFalse(IsValidResourceId(instance.SpeechSubscriptionKey, optional: false), "Failed to validate speech key");
            LogIfFalse(IsValidAzureRegion(instance.SpeechRegion), "Failed to validate Azure region");
            LogIfFalse(IsValidResourceId(instance.CustomSREndpointId, optional: true), "Failed to validate custom speech id");
            LogIfFalse(
                instance.CustomVoiceDeploymentIds.Split(',')
                    .All(customVoiceId => IsValidResourceId(customVoiceId, optional: true)),
                "Failed to validate custom voice ids");
            LogIfFalse(IsValidResourceId(instance.CustomCommandsAppId, optional: true), "Failed to validate Custom Commands id");
            LogIfFalse(IsValidModelPath(instance.KeywordActivationModel.Path), "Failed to validate keyword activation model path");
            LogIfFalse(IsValidModelPath(instance.KeywordRecognitionModel), "Failed to validate keyword confirmation model path");
        }
    }
}
