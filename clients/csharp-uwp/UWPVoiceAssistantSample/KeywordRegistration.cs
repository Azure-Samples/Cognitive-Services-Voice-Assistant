// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using UWPVoiceAssistantSample.KwsPerformance;
    using Windows.ApplicationModel.ConversationalAgent;
    using Windows.Storage;

    /// <summary>
    /// A helper class to encapsulate the process for registering a activation keyword.
    /// </summary>
    public class KeywordRegistration : IKeywordRegistration, IDisposable
    {
        private readonly SemaphoreSlim creatingKeywordConfigSemaphore;
        private ActivationSignalDetectionConfiguration softwareKeywordConfiguration;
        private ActivationSignalDetectionConfiguration hardwareKeywordConfiguration;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordRegistration"/> class.
        /// </summary>
        /// <param name="availableActivationKeywordModelVersion">Version of the most recent keyword model that is available.</param>
        public KeywordRegistration(Version availableActivationKeywordModelVersion)
        {
            this.AvailableActivationKeywordModelVersion = availableActivationKeywordModelVersion;

            this.creatingKeywordConfigSemaphore = new SemaphoreSlim(1, 1);

            _ = this.InitializeConfigurationsAsync();
        }

        /// <summary>
        /// Gets the display name associated with the keyword.
        /// </summary>
        public string KeywordDisplayName { get => LocalSettingsHelper.KeywordDisplayName; }

        /// <summary>
        /// Gets the signal identifier associated with a keyword. This value, together with the
        /// model identifier, uniquely identifies the configuration data for this keyword.
        /// </summary>
        public string KeywordId { get => LocalSettingsHelper.KeywordId; }

        /// <summary>
        /// Gets the model identifier associated with a keyword. This is typically a locale,
        /// like "1033", and together with the keyword identifier uniquely identifies the
        /// configuration data for this keyword.
        /// </summary>
        public string KeywordModelId { get => LocalSettingsHelper.KeywordModelId; }

        /// <summary>
        /// Gets the model data format associated with the activation keyword.
        /// </summary>
        public string KeywordActivationModelDataFormat { get => LocalSettingsHelper.KeywordActivationModelDataFormat; }

        /// <summary>
        /// Gets the path to the model data associated with your activation keyword. This may be a
        /// standard file path or an ms-appx:/// path pointing to a resource in the app package.
        /// When not provided, no attempt will be made to associate model data with the
        /// activation keyword.
        /// </summary>
        public string KeywordActivationModelFilePath { get => LocalSettingsHelper.KeywordActivationModelPath; }

        /// <summary>
        /// Gets or sets the available version of the model data associated with an activation keyword.
        /// </summary>
        public Version AvailableActivationKeywordModelVersion { get; set; }

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
            get
            {
                return LocalSettingsHelper.KeywordEnabledByApp;
            }

            set
            {
                LocalSettingsHelper.KeywordEnabledByApp = value;
                if (this.softwareKeywordConfiguration != null && this.softwareKeywordConfiguration.AvailabilityInfo.IsEnabled != value)
                {
                    this.softwareKeywordConfiguration.SetEnabledAsync(value).AsTask().Wait();
                }

                if (this.hardwareKeywordConfiguration != null && this.hardwareKeywordConfiguration.AvailabilityInfo.IsEnabled != value)
                {
                    this.hardwareKeywordConfiguration.SetEnabledAsync(value).AsTask().Wait();
                }
            }
        }

        /// <summary>
        /// Gets or sets the path to the keyword model used for validation of the activation
        /// keyword's result. This may be a file path or an ms-appx application path.
        /// </summary>
        public string ConfirmationKeywordModelPath { get => LocalSettingsHelper.KeywordRecognitionModel; set => this.ConfirmationKeywordModelPath = value; }

        /// <summary>
        /// Changes the registered keyword using values in the settings.
        /// </summary>
        /// <returns>A <see cref="Task"/> that returns on successful keyword setup.</returns>
        public async Task<List<ActivationSignalDetectionConfiguration>> GetOrCreateKeywordConfigurationsAsync()
        {
            return await this.ProcessConfigurationsAsync();
        }

        /// <summary>
        /// Changes the registered keyword using values in the settings.
        /// </summary>
        /// <returns>A <see cref="Task"/> that returns on successful keyword setup.</returns>
        public async Task<List<ActivationSignalDetectionConfiguration>> UpdateKeyword()
        {
            return await this.ProcessConfigurationsAsync();
        }

        /// <summary>
        /// Changes the registered keyword using values in the settings.
        /// </summary>
        /// <returns>A <see cref="Task"/> that returns on successful keyword setup.</returns>
        public async Task UpdateModelData()
        {
            using (await this.creatingKeywordConfigSemaphore.AutoReleaseWaitAsync())
            {
                if (this.softwareKeywordConfiguration != null)
                {
                    await this.SetModelDataIfNeededAsync(this.softwareKeywordConfiguration, true);
                }
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
            => this.GetFileFromPathAsync(this.KeywordActivationModelFilePath);

        /// <summary>
        /// Gets an asynchronous task that will return a storage file to the confirmation
        /// keyword associated with this registration information.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<StorageFile> GetConfirmationKeywordFileAsync()
            => this.GetFileFromPathAsync(this.ConfirmationKeywordModelPath);

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

                this.softwareKeywordConfiguration = null;
                this.hardwareKeywordConfiguration = null;
                this.AvailableActivationKeywordModelVersion = null;
                this.ConfirmationKeywordModelPath = string.Empty;

                this.disposed = true;
            }
        }

        private static async Task<ActivationSignalDetector> GetDetectorAsync(
            string dataFormat, bool canCreateConfigurations = true)
        {
            var detectorManager = ConversationalAgentDetectorManager.Default;
            var allDetectors = await detectorManager.GetAllActivationSignalDetectorsAsync();
            var detectors = allDetectors.Where(candidate => candidate.CanCreateConfigurations == canCreateConfigurations
                && candidate.Kind == ActivationSignalDetectorKind.AudioPattern
                && (string.IsNullOrEmpty(dataFormat) || candidate.SupportedModelDataTypes.Contains(dataFormat)));

            if (detectors.Count() != 1)
            {
                if (canCreateConfigurations)
                {
                    throw new NotSupportedException($"System expects one eligible configurable keyword spotter; actual is {detectors.Count()}.");
                }
                else
                {
                    throw new NotSupportedException($"System expects one eligible hardware keyword spotter; actual is {detectors.Count()}.");
                }
            }

            KwsPerformanceLogger.Spotter = "SWKWS";
            return detectors.First();
        }

        private static async Task<ActivationSignalDetectionConfiguration> GetOrCreateConfigurationOnDetectorAsync(
            ActivationSignalDetector detector,
            string displayName,
            string signalId,
            string modelId)
        {
            var configuration = await detector.GetConfigurationAsync(signalId, modelId);

            // Display name change requires config to be re-created, but it can only be done on detectors that support creating configurations.
            if (configuration != null && configuration.DisplayName != displayName && detector.CanCreateConfigurations)
            {
                if (configuration.AvailabilityInfo.IsEnabled)
                {
                    await configuration.SetEnabledAsync(false);
                }

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

        private async Task<List<ActivationSignalDetectionConfiguration>> ProcessConfigurationsAsync()
        {
            try
            {
                using (await this.creatingKeywordConfigSemaphore.AutoReleaseWaitAsync())
                {
                    this.softwareKeywordConfiguration = await this.GetOrCreateSoftwareKeywordConfigurationAsyncInternal();
                    this.hardwareKeywordConfiguration = await this.GetHardwareKeywordConfigurationAsyncInternal();
                }
            }
            catch (Exception ex)
            {
                string m = ex.Message;
            }

            List<ActivationSignalDetectionConfiguration> configurations = new List<ActivationSignalDetectionConfiguration>();
            if (this.softwareKeywordConfiguration != null)
            {
                configurations.Add(this.softwareKeywordConfiguration);
            }

            if (this.hardwareKeywordConfiguration != null)
            {
                configurations.Add(this.hardwareKeywordConfiguration);
            }

            return configurations;
        }

        private async Task InitializeConfigurationsAsync()
        {
            using (await this.creatingKeywordConfigSemaphore.AutoReleaseWaitAsync())
            {
                this.softwareKeywordConfiguration = await this.GetOrCreateSoftwareKeywordConfigurationAsyncInternal();
                this.hardwareKeywordConfiguration = await this.GetHardwareKeywordConfigurationAsyncInternal();
            }
        }

        private async Task SetModelDataIfNeededAsync(ActivationSignalDetectionConfiguration configuration, bool isModelDataUpdate)
        {
            if (!LocalSettingsHelper.SetModelData)
            {
                return;
            }

            if (!isModelDataUpdate && this.LastUpdatedActivationKeywordModelVersion.CompareTo(this.AvailableActivationKeywordModelVersion) >= 0)
            {
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
                var configurationWasEnabled = configuration.AvailabilityInfo.IsEnabled;

                if (configurationWasEnabled)
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

                // And now, re-enable the configuration if it was previously enabled.
                if (configurationWasEnabled)
                {
                    await configuration.SetEnabledAsync(true);
                }
            }
        }

        private async Task<bool> PrepareConfigurationUpdate(ActivationSignalDetector detector, ActivationSignalDetectionConfiguration currentConfiguration, bool detectorEnabled)
        {
            bool updateNeeded = false;

            // If we have current configuration check if anything needs to be changed regarding that.
            if (currentConfiguration == null && detector != null)
            {
                updateNeeded = true;
            }
            else if (currentConfiguration != null)
            {
                if (!detectorEnabled)
                {
                    if (currentConfiguration.AvailabilityInfo.IsEnabled)
                    {
                        await currentConfiguration.SetEnabledAsync(false);
                        updateNeeded = true;
                    }
                }
                else
                {
                    if (detector != null)
                    {
                        // If we previoulsy already had a configuration, make sure all other configurations on the detector (except one matching current configuration) are disabled first.
                        var configurations = await detector.GetConfigurationsAsync();
                        foreach (var configuration in configurations)
                        {
                            if (((this.KeywordId != configuration.SignalId) || (this.KeywordModelId != configuration.ModelId)) && configuration.AvailabilityInfo.IsEnabled)
                            {
                                await configuration.SetEnabledAsync(false);
                            }
                        }
                    }

                    if ((this.KeywordId != currentConfiguration.SignalId) || (this.KeywordModelId != currentConfiguration.ModelId))
                    {
                        updateNeeded = true;
                    }
                    else if (detector.CanCreateConfigurations && (this.KeywordDisplayName != currentConfiguration.DisplayName))
                    {
                        updateNeeded = true;
                    }
                    else if (this.LastUpdatedActivationKeywordModelVersion.CompareTo(this.AvailableActivationKeywordModelVersion) < 0)
                    {
                        updateNeeded = true;
                    }
                }
            }

            return updateNeeded;
        }

        private async Task<ActivationSignalDetectionConfiguration> GetHardwareKeywordConfigurationAsyncInternal()
        {
            ActivationSignalDetector hardwareKeywordDetector = null;
            try
            {
                hardwareKeywordDetector = await GetDetectorAsync(string.Empty, false);
            }
            catch (NotSupportedException)
            {
            }

            bool updateNeeded = await this.PrepareConfigurationUpdate(hardwareKeywordDetector, this.hardwareKeywordConfiguration, LocalSettingsHelper.UseHardwareDetector);
            if (updateNeeded)
            {
                this.hardwareKeywordConfiguration = null;
                if (LocalSettingsHelper.UseHardwareDetector)
                {
                    if (hardwareKeywordDetector != null)
                    {
                        this.hardwareKeywordConfiguration = await hardwareKeywordDetector.GetConfigurationAsync(this.KeywordId, this.KeywordModelId);
                    }

                    if (this.hardwareKeywordConfiguration != null)
                    {
                        if (this.hardwareKeywordConfiguration.AvailabilityInfo.IsEnabled != this.KeywordEnabledByApp)
                        {
                            await this.hardwareKeywordConfiguration.SetEnabledAsync(this.KeywordEnabledByApp);
                        }
                    }
                }
            }

            return this.hardwareKeywordConfiguration;
        }

        private async Task<ActivationSignalDetectionConfiguration> GetOrCreateSoftwareKeywordConfigurationAsyncInternal()
        {
            ActivationSignalDetector softwareKeywordDetector = null;
            try
            {
                softwareKeywordDetector = await GetDetectorAsync(this.KeywordActivationModelDataFormat);
            }
            catch (NotSupportedException)
            {
            }

            bool updateNeeded = await this.PrepareConfigurationUpdate(softwareKeywordDetector, this.softwareKeywordConfiguration, LocalSettingsHelper.UseSoftwareDetector);
            if (updateNeeded)
            {
                this.softwareKeywordConfiguration = null;
                if (LocalSettingsHelper.UseSoftwareDetector && softwareKeywordDetector != null)
                {
                    this.softwareKeywordConfiguration = await GetOrCreateConfigurationOnDetectorAsync(
                        softwareKeywordDetector,
                        this.KeywordDisplayName,
                        this.KeywordId,
                        this.KeywordModelId);
                    await this.SetModelDataIfNeededAsync(this.softwareKeywordConfiguration, false);
                    if (this.softwareKeywordConfiguration.AvailabilityInfo.IsEnabled != this.KeywordEnabledByApp)
                    {
                        await this.softwareKeywordConfiguration.SetEnabledAsync(this.KeywordEnabledByApp);
                    }
                }
            }

            return this.softwareKeywordConfiguration;
        }

        private async Task<StorageFile> GetFileFromPathAsync(string path)
            => path.StartsWith("ms-app", StringComparison.InvariantCultureIgnoreCase)
                ? await StorageFile.GetFileFromApplicationUriAsync(new Uri(path))
                : await StorageFile.GetFileFromPathAsync(path);
    }
}
