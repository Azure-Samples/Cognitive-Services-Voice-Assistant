// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.ConversationalAgent;
    using Windows.Security.Cryptography.Core;
    using Windows.Storage;

    /// <summary>
    /// A helper class to encapsulate the process for activation keywords.
    /// </summary>
    public class KeywordRegistration
    {
        /// <summary>
        /// Gets the keyword registration information associated with the built-in "Contoso"
        /// keyword for the sample application.
        /// </summary>
        public static KeywordRegistration Contoso => new KeywordRegistration()
        {
            KeywordDisplayName = "Contoso",
            KeywordId = "{C0F1842F-D389-44D1-8420-A32A63B35568}",
            KeywordModelId = "1033",
            KeywordActivationModelDataFormat = "MICROSOFT_KWSGRAPH_V1",
            KeywordActivationModelFilePath = "ms-appx:///MVAKeywords/Contoso.bin",
            AvailableActivationKeywordModelVersion = new Version(1, 0, 0, 0),
            ConfirmationKeywordModelPath = "ms-appx:///SDKKeywords/Contoso.table",
        };

        /// <summary>
        /// Gets the keyword registration for the "Computer" keyword, which is a test keyword
        /// whose activation resources are included in the operating system.
        /// </summary>
        public static KeywordRegistration Computer => new KeywordRegistration()
        {
            KeywordDisplayName = "Computer",
            KeywordId = "{A5A7C794-3D59-41DF-915F-19ACDA526FC9}",
            KeywordModelId = "1033",
            ConfirmationKeywordModelPath = "ms-appx:///SDKKeywords/computer.table",
        };

        /// <summary>
        /// Gets the registration information for the "Xbox" keyword. The activation keyword
        /// resources for this are built into the operating system and only confirmation models
        /// are needed.
        /// </summary>
        public static KeywordRegistration Xbox => new KeywordRegistration()
        {
            KeywordDisplayName = "Xbox",
            KeywordId = "{D3E34B12-11AC-4F22-A7D7-180F18F3EDD9}",
            KeywordModelId = "1033",
            ConfirmationKeywordModelPath = "ms-appx:///SDKKeywords/xbox.table",
        };

        /// <summary>
        /// Gets the display name associated with the keyword.
        /// </summary>
        public string KeywordDisplayName { get; private set; }

        /// <summary>
        /// Gets the signal identifier associated with a keyword. This value, together with the
        /// model identifier, uniquely identifies the configuration data for this keyword.
        /// </summary>
        public string KeywordId { get; private set; }

        /// <summary>
        /// Gets the model identifier associated with a keyword. This is typically a locale,
        /// like "1033", and together with the keyword identifier uniquely identifies the
        /// configuration data for this keyword.
        /// </summary>
        public string KeywordModelId { get; private set; }

        /// <summary>
        /// Gets the model data format associated with the activation keyword.
        /// </summary>
        public string KeywordActivationModelDataFormat { get; private set; }

        /// <summary>
        /// Gets the path to the model data associated with your activation keyword. This may be a
        /// standard file path or an ms-appx:/// path pointing to a resource in the app package.
        /// When not provided, no attempt will be made to associate model data with the
        /// activation keyword.
        /// </summary>
        public string KeywordActivationModelFilePath { get; private set; }

        /// <summary>
        /// Gets the available version of the model data associated with an activation keyword.
        /// </summary>
        public Version AvailableActivationKeywordModelVersion { get; private set; }

        /// <summary>
        /// Gets or sets the last successfully updated model data version associated with the
        /// activation keyword. This is stored in local settings for later retrieval when the
        /// data for a keyword may change and need update.
        /// </summary>
        public Version LastUpdatedActivationKeywordModelVersion
        {
            get
            {
                var key = $"lastKwModelVer_{this.KeywordId}:{this.KeywordModelId}";
                var value = LocalSettingsHelper.ReadValueWithDefault<string>(key, "0.0.0.0");
                return Version.Parse(value);
            }

            set
            {
                Contract.Requires(value != null);
                var key = $"lastKwModelVer_{this.KeywordId}:{this.KeywordModelId}";
                LocalSettingsHelper.WriteValue(key, value.ToString());
            }
        }

        /// <summary>
        /// Gets  sets the path to the keyword model used for validation of the activation
        /// keyword's result. This may be a file path or an ms-appx application path.
        /// </summary>
        public string ConfirmationKeywordModelPath { get; private set; }

        /// <summary>
        /// Fetches and, if necessary, creates an activation keyword configuration matching the
        /// specified keyword registration information.
        /// </summary>
        /// <param name="keyword"> The data representing the activation keyword configuration that should be created, either as specified in code or within a configuration file. </param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<ActivationSignalDetectionConfiguration> CreateActivationKeywordConfigurationAsync(KeywordRegistration keyword)
        {
            Contract.Requires(keyword != null);

            var detector = await GetFirstEligibleDetectorAsync(keyword.KeywordActivationModelDataFormat);
            var targetConfiguration = await GetOrCreateConfigurationOnDetectorAsync(
                detector,
                keyword.KeywordDisplayName,
                keyword.KeywordId,
                keyword.KeywordModelId);
            await SetModelDataIfNeededAsync(targetConfiguration, keyword);

            return targetConfiguration;
        }

        /// <summary>
        /// Creates a keyword registration data collection from an input file, such as a
        /// manifest for keyword updates.
        /// </summary>
        /// <returns> A configuration object for keyword registration. </returns>
        public static KeywordRegistration CreateFromFileAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets an asynchronous task that will return a storage file to the activation
        /// keyword associated with this registration information.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<StorageFile> GetActivationKeywordFileAsync()
            => GetFileFromPathAsync(this.KeywordActivationModelFilePath);

        /// <summary>
        /// Gets an asynchronous task that will return a storage file to the confirmation
        /// keyword associated with this registration information.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<StorageFile> GetConfirmationKeywordFileAsync()
            => GetFileFromPathAsync(this.ConfirmationKeywordModelPath);

        private static async Task<ActivationSignalDetector> GetFirstEligibleDetectorAsync(
            string dataFormat)
        {
            var detectorManager = ConversationalAgentDetectorManager.Default;
            var allDetectors = await detectorManager.GetAllActivationSignalDetectorsAsync();
            var configurableDetectors = allDetectors.Where(candidate => candidate.CanCreateConfigurations
                && candidate.Kind == ActivationSignalDetectorKind.AudioPattern
                && (string.IsNullOrEmpty(dataFormat) || candidate.SupportedModelDataTypes.Contains(dataFormat)));

            if (configurableDetectors.Count() != 1)
            {
                throw new NotSupportedException($"System expects one eligible configurable keyword spotter; actual is {configurableDetectors.Count()}.");
            }

            var detector = configurableDetectors.First();

            return detector;
        }

        private static async Task<ActivationSignalDetectionConfiguration> GetOrCreateConfigurationOnDetectorAsync(
            ActivationSignalDetector detector,
            string displayName,
            string signalId,
            string modelId)
        {
            var configuration = await detector.GetConfigurationAsync(signalId, modelId);

            if (configuration != null && configuration.DisplayName != displayName)
            {
                await detector.RemoveConfigurationAsync(signalId, modelId);
                configuration = null;
            }

            if (configuration == null)
            {
                await detector.CreateConfigurationAsync(signalId, modelId, displayName);
                configuration = await detector.GetConfigurationAsync(signalId, modelId);
            }

            return configuration;
        }

        private static async Task SetModelDataIfNeededAsync(
            ActivationSignalDetectionConfiguration configuration,
            KeywordRegistration keyword)
        {
            if (keyword.LastUpdatedActivationKeywordModelVersion.CompareTo(keyword.AvailableActivationKeywordModelVersion) >= 0)
            {
                // Keyword is already up to date according to this data; nothing to do here!
                return;
            }

            var hasNoFormat = string.IsNullOrEmpty(keyword.KeywordActivationModelDataFormat);
            var hasNoPath = string.IsNullOrEmpty(keyword.KeywordActivationModelFilePath);

            // Low-frequency operator: ^ is 'exclusive or'
            if (hasNoFormat ^ hasNoPath)
            {
                throw new ArgumentException("Model data type and path must both be provided (when setting data) or both be empty (when not setting data)");
            }
            else if (!hasNoFormat && !hasNoPath)
            {
                if (configuration.AvailabilityInfo.IsEnabled)
                {
                    // Active configurations can't have their data updated. Disable for now;
                    // we'll re-enable shortly.
                    await configuration.SetEnabledAsync(false);
                }

                // await configuration.ClearModelDataAsync();
                var modelDataFile = await keyword.GetActivationKeywordFileAsync();
                using (var modelDataStream = await modelDataFile.OpenSequentialReadAsync())
                {
                    await configuration.SetModelDataAsync(keyword.KeywordActivationModelDataFormat, modelDataStream);
                }

                // Update was successful. Record this so we don't repeat it needlessly!
                keyword.LastUpdatedActivationKeywordModelVersion = keyword.AvailableActivationKeywordModelVersion;

                // And now, re-enable the configuration.
                await configuration.SetEnabledAsync(true);
            }
        }

        private static async Task<StorageFile> GetFileFromPathAsync(string path)
            => path.StartsWith("ms-appx", StringComparison.InvariantCultureIgnoreCase)
                ? await StorageFile.GetFileFromApplicationUriAsync(new Uri(path))
                : await StorageFile.GetFileFromPathAsync(path);
    }
}
