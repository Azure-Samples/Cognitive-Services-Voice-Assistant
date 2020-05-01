// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSampleTests
{
    using System;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.ConversationalAgent;
    using Windows.Storage;
    using UWPVoiceAssistantSample;

    /// <summary>
    /// Mock of KeywordRegistration
    /// </summary>
    public class MockKeywordRegistration : IKeywordRegistration
    {
        public string KeywordDisplayName => string.Empty;

        public string KeywordId => string.Empty;

        public string KeywordModelId => string.Empty;

        public string KeywordActivationModelDataFormat => string.Empty;

        public string KeywordActivationModelFilePath => string.Empty;

        public Version AvailableActivationKeywordModelVersion { get; private set; }

        public Version LastUpdatedActivationKeywordModelVersion { get; set; }

        public bool KeywordEnabledByApp { get; set; }

        public string ConfirmationKeywordModelPath => string.Empty;

        public KeywordRegistration CreateFromFileAsync()
        {
            return null;
        }

        public Task<ActivationSignalDetectionConfiguration> CreateKeywordConfigurationAsync()
        {
            return null;
        }

        public async Task<StorageFile> GetActivationKeywordFileAsync()
        {
            return null;
        }

        public async Task<StorageFile> GetConfirmationKeywordFileAsync()
        {
            return null;
        }

        public async Task<ActivationSignalDetectionConfiguration> GetOrCreateKeywordConfigurationAsync()
        {
            return null;
        }

        public async Task<ActivationSignalDetectionConfiguration> UpdateKeyword(string keywordDisplayName, string keywordId, string keywordModelId, string keywordActivationModelDataFormat, string keywordActivationModelFilePath, Version availableActivationKeywordModelVersion, string confirmationKeywordModelPath)
        {
            return null;
        }
    }
}
