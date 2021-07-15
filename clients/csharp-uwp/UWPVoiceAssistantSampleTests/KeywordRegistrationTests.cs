// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace KeywordRegistrationTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using UWPVoiceAssistantSample;
    using Windows.Storage;

    [TestClass]
    public class KeywordRegistrationTests
    {
        private KeywordRegistration keywordRegistration = new KeywordRegistration(new Version(1, 0, 0, 0));


        [TestMethod]
        public async Task RunKeywordRegistrationTestsSequentially()
        {
            await LastUpdatedActivationKeywordModelVersionAsyncTest();
            LastUpdatedActivationKeywordModelVersionGreaterThanCurrentVersionTest();
            await UpdateKeywordAsyncTest();
            await GetOrCreateKeywordConfigurationAsyncTest();
            await GetActivationKeywordFileAsyncTest();
            await GetConfirmationFileAsyncTest();
            await VerifyActiviationKeywordFilePresentAsyncTest();
            await VerifyActivationAndConfirmationPathsAsyncTest();
        }

        public async Task LastUpdatedActivationKeywordModelVersionAsyncTest()
        {
            var lastKeywordModelVersion = this.keywordRegistration.LastUpdatedActivationKeywordModelVersion;
            var configurations = await keywordRegistration.GetOrCreateKeywordConfigurationsAsync();
            var getOrCreateConfiguration = configurations[0];

            if (this.keywordRegistration.KeywordId == getOrCreateConfiguration.SignalId &&
                lastKeywordModelVersion == this.keywordRegistration.AvailableActivationKeywordModelVersion)
            {
                string message = "Keyword is up to date";
                Assert.AreEqual(this.keywordRegistration.AvailableActivationKeywordModelVersion, lastKeywordModelVersion, message);
            }
            else
            {
                var updatedConfigurations = await this.keywordRegistration.UpdateKeyword();
                var updateKeyword = updatedConfigurations[0];
                Assert.AreEqual(this.keywordRegistration.LastUpdatedActivationKeywordModelVersion, this.keywordRegistration.AvailableActivationKeywordModelVersion);
                Assert.IsTrue(updateKeyword.IsActive);
                Assert.AreEqual(this.keywordRegistration.KeywordDisplayName, updateKeyword.DisplayName);
                Assert.AreEqual(this.keywordRegistration.KeywordModelId, updateKeyword.ModelId);
                Assert.AreEqual(this.keywordRegistration.KeywordId, updateKeyword.SignalId);
                Assert.IsTrue(updateKeyword.AvailabilityInfo.HasPermission);
                Assert.IsTrue(updateKeyword.AvailabilityInfo.HasSystemResourceAccess);
                Assert.IsTrue(updateKeyword.AvailabilityInfo.IsEnabled);
            }
        }

        public void LastUpdatedActivationKeywordModelVersionGreaterThanCurrentVersionTest()
        {
            var lastKeywordModelVersion = this.keywordRegistration.LastUpdatedActivationKeywordModelVersion;

            if (lastKeywordModelVersion >= this.keywordRegistration.AvailableActivationKeywordModelVersion)
            {
                Assert.IsTrue(lastKeywordModelVersion >= this.keywordRegistration.AvailableActivationKeywordModelVersion);
            }
            else
            {
                throw new Exception($"LastUpdatedActivationKeywordModelVersion is greater than current version: {this.keywordRegistration.AvailableActivationKeywordModelVersion}");
            }
        }

        public async Task UpdateKeywordAsyncTest()
        {
            var results = await this.keywordRegistration.UpdateKeyword();
            var result = results[0];

            Assert.IsTrue(result.AvailabilityInfo.HasPermission);
            Assert.IsTrue(result.AvailabilityInfo.HasSystemResourceAccess);
            Assert.IsTrue(result.AvailabilityInfo.IsEnabled);
            Assert.AreEqual(this.keywordRegistration.KeywordDisplayName, result.DisplayName);
            Assert.IsTrue(result.IsActive);
            Assert.AreEqual(this.keywordRegistration.KeywordModelId, result.ModelId);
            Assert.AreEqual(this.keywordRegistration.KeywordId, result.SignalId);
        }

        public async Task GetOrCreateKeywordConfigurationAsyncTest()
        {
            var results = await this.keywordRegistration.GetOrCreateKeywordConfigurationsAsync();
            var result = results[0];

            Assert.IsTrue(result.AvailabilityInfo.HasPermission);
            Assert.IsTrue(result.AvailabilityInfo.HasSystemResourceAccess);
            Assert.IsTrue(result.AvailabilityInfo.IsEnabled);
            Assert.AreEqual(this.keywordRegistration.KeywordDisplayName, result.DisplayName);
            Assert.IsTrue(result.IsActive);
            Assert.AreEqual(this.keywordRegistration.KeywordModelId, result.ModelId);
            Assert.AreEqual(this.keywordRegistration.KeywordId, result.SignalId);
        }

        public async Task GetActivationKeywordFileAsyncTest()
        {
            var result = await this.keywordRegistration.GetActivationKeywordFileAsync();

            var MVAKeywordPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets\\ActivationKeywords\\Contoso.bin");

            Assert.AreEqual(this.keywordRegistration.KeywordDisplayName + ".bin", result.DisplayName);
            Assert.AreEqual("BIN File", result.DisplayType);
            Assert.AreEqual(".bin", result.FileType);
            Assert.IsTrue(result.IsAvailable);
            Assert.AreEqual(this.keywordRegistration.KeywordDisplayName + ".bin", result.Name);
            Assert.AreEqual(MVAKeywordPath, result.Path);

            if (string.IsNullOrEmpty(MVAKeywordPath) || !File.Exists(MVAKeywordPath))
            {
                throw new ArgumentException($"Activation Keyword File is not found");
            }
        }

        public async Task GetConfirmationFileAsyncTest()
        {
            var result = await this.keywordRegistration.GetConfirmationKeywordFileAsync();

            var ConfirmationKeywordPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets\\ConfirmationKeywords\\contoso.table");

            string displayName = char.ToUpper(result.DisplayName[0]) + result.DisplayName.Substring(1, result.DisplayName.Length - 1);
            Assert.AreEqual(this.keywordRegistration.KeywordDisplayName + ".table", displayName);
            Assert.AreEqual("TABLE File", result.DisplayType);
            Assert.AreEqual(".table", result.FileType);
            Assert.IsTrue(result.IsAvailable);
            string name = char.ToUpper(result.Name[0]) + result.Name.Substring(1, result.Name.Length - 1);
            Assert.AreEqual(this.keywordRegistration.KeywordDisplayName + ".table", name);
            Assert.AreEqual(ConfirmationKeywordPath, result.Path);
        }

        public async Task VerifyActiviationKeywordFilePresentAsyncTest()
        {
            var MVAKeywordFile = Path.Combine(Directory.GetCurrentDirectory(), "Assets\\ActivationKeywords\\Contoso.bin");

            var result = await GetStorageFile(this.keywordRegistration.KeywordActivationModelFilePath);

            var fileExists = File.Exists(MVAKeywordFile);

            if (fileExists && result.Path == MVAKeywordFile)
            {
                var message = $"Activation Keyword File is present at location: ${MVAKeywordFile}";
                Assert.IsTrue(true, message);
                Assert.AreEqual(result.Path, MVAKeywordFile);
            }
            else
            {
                var message = $"Could not find Activation Keyword File";
                Assert.Fail(message);
            }
        }

        public async Task VerifyActivationAndConfirmationPathsAsyncTest()
        {
            string activationKeywordPath = this.keywordRegistration.KeywordActivationModelFilePath;
            string confirmationModelPath = this.keywordRegistration.ConfirmationKeywordModelPath;
            bool activationKeywordPathNull = string.IsNullOrEmpty(activationKeywordPath);
            bool confirmationModelPathNull = string.IsNullOrEmpty(confirmationModelPath);

            if (!activationKeywordPathNull)
            {
                var activationKeywordLocation = await GetStorageFile(activationKeywordPath);

                if (!File.Exists(activationKeywordLocation.Path))
                {
                    throw new ArgumentException("Keyword Activation Model File Path does not exist");
                }
            }
            else if (activationKeywordPathNull)
            {
                throw new ArgumentException("Keyword Activation Model File Path is null");
            }

            if (!confirmationModelPathNull)
            {
                var confirmationModelLocation = await GetStorageFile(confirmationModelPath);

                if (!File.Exists(confirmationModelLocation.Path))
                {
                    throw new ArgumentException("Confirmation Model File Path does not exist");
                }
            }
            else if (confirmationModelPathNull)
            {
                throw new ArgumentException("Confirmation Model File Path is null");
            }
        }

        private async Task<StorageFile> GetStorageFile(string path)
        {
            if (path.StartsWith("ms-appx"))
            {
                return await StorageFile.GetFileFromApplicationUriAsync(new Uri(path));
            }
            else
            {
                return await StorageFile.GetFileFromPathAsync(path);
            }
        }
    }
}
