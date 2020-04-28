// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.ConversationalAgent;
    using Windows.Storage;

    /// <summary>
    /// A helper class to encapsulate the process for registering a activation keyword.
    /// </summary>
    public class KeywordRegistration : IKeywordRegistration, IDisposable
    {
        private readonly SemaphoreSlim creatingKeywordConfigSemaphore;
        private ActivationSignalDetectionConfiguration keywordConfiguration;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordRegistration"/> class.
        /// </summary>
        /// <param name="keywordDisplayName">Display name shown for the keyword in settings.</param>
        /// <param name="keywordId">Id of the keyword.</param>
        /// <param name="keywordModelId">Model id of the keyword.</param>
        /// <param name="keywordActivationModelDataFormat">Data format of the keyword activation model.</param>
        /// <param name="keywordActivationModelFilePath">File path of the keyword activation model.</param>
        /// <param name="availableActivationKeywordModelVersion">Version of the most recent keyword model that is available.</param>
        /// <param name="confirmationKeywordModelPath">Path of the confirmation keyword model.</param>
        public KeywordRegistration(
            string keywordDisplayName,
            string keywordId,
            string keywordModelId,
            string keywordActivationModelDataFormat,
            string keywordActivationModelFilePath,
            Version availableActivationKeywordModelVersion,
            string confirmationKeywordModelPath)
        {
            this.KeywordDisplayName = keywordDisplayName;
            this.KeywordId = keywordId;
            this.KeywordModelId = keywordModelId;
            this.KeywordActivationModelDataFormat = keywordActivationModelDataFormat;
            this.KeywordActivationModelFilePath = keywordActivationModelFilePath;
            this.AvailableActivationKeywordModelVersion = availableActivationKeywordModelVersion;
            this.ConfirmationKeywordModelPath = confirmationKeywordModelPath;

            this.creatingKeywordConfigSemaphore = new SemaphoreSlim(1, 1);

            _ = this.GetOrCreateKeywordConfigurationAsync();
        }

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
        public string KeywordActivationModelFilePath
        {
            get; private set;
        }

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
        /// Gets or sets a value indicating whether the current active keyword configuration is
        /// in an application-enabled state.
        /// </summary>
        public bool KeywordEnabledByApp
        {
            get => this.keywordConfiguration?.AvailabilityInfo?.IsEnabled ?? false;
            set => this.keywordConfiguration?.SetEnabledAsync(value).AsTask().Wait();
        }

        /// <summary>
        /// Gets  sets the path to the keyword model used for validation of the activation
        /// keyword's result. This may be a file path or an ms-appx application path.
        /// </summary>
        public string ConfirmationKeywordModelPath { get; private set; }

        /// <summary>
        /// Changes the registered keyword using the new inputs.
        /// </summary>
        /// <param name="keywordDisplayName">Display name shown for the keyword in settings.</param>
        /// <param name="keywordId">Id of the keyword.</param>
        /// <param name="keywordModelId">Model id of the keyword.</param>
        /// <param name="keywordActivationModelDataFormat">Data format of the keyword activation model.</param>
        /// <param name="keywordActivationModelFilePath">File path of the keyword activation model.</param>
        /// <param name="availableActivationKeywordModelVersion">Version of the most recent keyword model that is available.</param>
        /// <param name="confirmationKeywordModelPath">Path of the confirmation keyword model.</param>
        /// <returns>A <see cref="Task"/> that returns on successful keyword setup.</returns>
        public async Task<ActivationSignalDetectionConfiguration> UpdateKeyword(
            string keywordDisplayName,
            string keywordId,
            string keywordModelId,
            string keywordActivationModelDataFormat,
            string keywordActivationModelFilePath,
            Version availableActivationKeywordModelVersion,
            string confirmationKeywordModelPath)
        {
            this.KeywordDisplayName = keywordDisplayName;
            this.KeywordId = keywordId;
            this.KeywordModelId = keywordModelId;
            this.KeywordActivationModelDataFormat = keywordActivationModelDataFormat;
            this.KeywordActivationModelFilePath = keywordActivationModelFilePath;
            this.AvailableActivationKeywordModelVersion = availableActivationKeywordModelVersion;
            this.ConfirmationKeywordModelPath = confirmationKeywordModelPath;

            this.keywordConfiguration = null;
            return await this.GetOrCreateKeywordConfigurationAsync();
        }

        /// <summary>
        /// Fetches and, if necessary, creates an activation keyword configuration matching the
        /// specified keyword registration information.
        /// </summary>
        /// <returns>A <see cref="Task"/> that returns on successful keyword setup.</returns>
        public async Task<ActivationSignalDetectionConfiguration> GetOrCreateKeywordConfigurationAsync()
        {
            using (await this.creatingKeywordConfigSemaphore.AutoReleaseWaitAsync())
            {
                if (this.keywordConfiguration != null)
                {
                    return this.keywordConfiguration;
                }

                var detector = await GetFirstEligibleDetectorAsync(this.KeywordActivationModelDataFormat);
                var targetConfiguration = await GetOrCreateConfigurationOnDetectorAsync(
                    detector,
                    this.KeywordDisplayName,
                    this.KeywordId,
                    this.KeywordModelId);
                await this.SetModelDataIfNeededAsync(targetConfiguration);

                this.keywordConfiguration = targetConfiguration;

                return targetConfiguration;
            }
        }

        /// <summary>
        /// Creates a keyword registration data collection from an input file, such as a
        /// manifest for keyword updates.
        /// </summary>
        /// <returns> A configuration object for keyword registration. </returns>
        public KeywordRegistration CreateFromFileAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets an asynchronous task that will return a storage file to the activation
        /// keyword associated with this registration information.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<StorageFile> GetActivationKeywordFileAsync()
        {
            this.CopyActivationFileFromPath(LocalSettingsHelper.KeywordActivationPath);
            var fileName = Path.GetFileName(this.KeywordActivationModelFilePath);
            return this.GetFileFromPathAsync(fileName);
        }

        /// <summary>
        /// Gets an asynchronous task that will return a storage file to the confirmation
        /// keyword associated with this registration information.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<StorageFile> GetConfirmationKeywordFileAsync()
        {
            this.CopyConfirmationFileFromPath(LocalSettingsHelper.KeywordConfirmationModelPath);
            var fileName = Path.GetFileName(this.ConfirmationKeywordModelPath);
            return this.GetFileFromPathAsync(fileName);
        }

        /// <summary>
        /// Default Dispose implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Handle the IDisposable pattern, specifically for the managed resources here.
        /// </summary>
        /// <param name="disposing"> whether managed resources are being disposed. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.creatingKeywordConfigSemaphore?.Dispose();
                }

                this.keywordConfiguration = null;
                this.KeywordDisplayName = string.Empty;
                this.KeywordId = string.Empty;
                this.KeywordModelId = string.Empty;
                this.KeywordActivationModelDataFormat = string.Empty;
                this.KeywordActivationModelFilePath = string.Empty;
                this.AvailableActivationKeywordModelVersion = null;
                this.ConfirmationKeywordModelPath = string.Empty;

                this.disposed = true;
            }
        }

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

        private async Task SetModelDataIfNeededAsync(ActivationSignalDetectionConfiguration configuration)
        {
            if (this.LastUpdatedActivationKeywordModelVersion.CompareTo(this.AvailableActivationKeywordModelVersion) >= 0)
            {
                // Keyword is already up to date according to this data; nothing to do here!
                return;
            }

            var hasNoFormat = string.IsNullOrEmpty(this.KeywordActivationModelDataFormat);
            var hasNoPath = string.IsNullOrEmpty(this.KeywordActivationModelFilePath);

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
                var modelDataFile = await this.GetActivationKeywordFileAsync();
                using (var modelDataStream = await modelDataFile.OpenSequentialReadAsync())
                {
                    await configuration.SetModelDataAsync(this.KeywordActivationModelDataFormat, modelDataStream);
                }

                // Update was successful. Record this so we don't repeat it needlessly!
                this.LastUpdatedActivationKeywordModelVersion = this.AvailableActivationKeywordModelVersion;

                // And now, re-enable the configuration.
                await configuration.SetEnabledAsync(true);
            }
        }

        private async Task<StorageFile> GetFileFromPathAsync(string path)
            => path.StartsWith("ms-appx", StringComparison.InvariantCultureIgnoreCase)
                ? await StorageFile.GetFileFromApplicationUriAsync(new Uri(path))
                : await StorageFile.GetFileFromPathAsync(path);

        private void CopyActivationFileFromPath(string path)
        {
            File.Copy(path, Directory.GetCurrentDirectory() + "//MVAKeywords//", true);
        }

        private void CopyConfirmationFileFromPath(string path)
        {
            //var fileName = Path.GetDirectoryName(path);
            //var item = Directory.GetFiles(fileName);
            File.Copy(path, Path.Combine(Directory.GetCurrentDirectory(), "SDKKeywords") + Path.GetFileName(path), true);
            //foreach (var file in item)
            //{
            //    File.Copy(file, Path.Combine(Directory.GetCurrentDirectory(), "SDKKeywords"), true);
            //}
            
            
        }
    }
}
