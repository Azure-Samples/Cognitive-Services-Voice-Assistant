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
        private KeywordRegistration keywordRegistration = new KeywordRegistration(
                "Contoso",
                "{C0F1842F-D389-44D1-8420-A32A63B35568}",
                "1033",
                "MICROSOFT_KWSGRAPH_V1",
                new Version(1, 0, 0, 0));


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
            await VerifyAppropriateValuesAsyncTest();
        }

        public async Task LastUpdatedActivationKeywordModelVersionAsyncTest()
        {
            var lastKeywordModelVersion = this.keywordRegistration.LastUpdatedActivationKeywordModelVersion;
            var getOrCreateConfiguration = await keywordRegistration.GetOrCreateKeywordConfigurationAsync();

            if (this.keywordRegistration.KeywordId == getOrCreateConfiguration.SignalId &&
                lastKeywordModelVersion == this.keywordRegistration.AvailableActivationKeywordModelVersion)
            {
                string message = "Keyword is up to date";
                Assert.AreEqual(this.keywordRegistration.AvailableActivationKeywordModelVersion, lastKeywordModelVersion, message);
            }
            else
            {
                var updateKeyword = await this.keywordRegistration.UpdateKeyword(
                    this.keywordRegistration.KeywordDisplayName,
                    this.keywordRegistration.KeywordId,
                    this.keywordRegistration.KeywordModelId,
                    this.keywordRegistration.KeywordActivationModelDataFormat,
                    this.keywordRegistration.KeywordActivationModelFilePath,
                    this.keywordRegistration.AvailableActivationKeywordModelVersion,
                    this.keywordRegistration.ConfirmationKeywordModelPath);
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
            var result = await this.keywordRegistration.UpdateKeyword(
                this.keywordRegistration.KeywordDisplayName,
                this.keywordRegistration.KeywordId,
                this.keywordRegistration.KeywordModelId,
                this.keywordRegistration.KeywordActivationModelDataFormat,
                this.keywordRegistration.KeywordActivationModelFilePath,
                this.keywordRegistration.AvailableActivationKeywordModelVersion,
                this.keywordRegistration.ConfirmationKeywordModelPath);

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
            var result = await this.keywordRegistration.GetOrCreateKeywordConfigurationAsync();

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

            var MVAKeywordPath = Path.Combine(Directory.GetCurrentDirectory(), "MVAKeywords\\Contoso.bin");

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

            var ConfirmationKeywordPath = Path.Combine(Directory.GetCurrentDirectory(), "SDKKeywords\\Contoso.table");

            Assert.AreEqual(this.keywordRegistration.KeywordDisplayName + ".table", result.DisplayName);
            Assert.AreEqual("TABLE File", result.DisplayType);
            Assert.AreEqual(".table", result.FileType);
            Assert.IsTrue(result.IsAvailable);
            Assert.AreEqual(this.keywordRegistration.KeywordDisplayName + ".table", result.Name);
            Assert.AreEqual(ConfirmationKeywordPath, result.Path);
        }

        public async Task VerifyActiviationKeywordFilePresentAsyncTest()
        {
            var MVAKeywordFile = Path.Combine(Directory.GetCurrentDirectory(), "MVAKeywords\\Contoso.bin");

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

        public async Task VerifyAppropriateValuesAsyncTest()
        {
            KeywordRegistration keyword = new KeywordRegistration(
                            "Contoso",
                            "{C0F1842F-D389-44D1-8420-A32A63B35568}",
                            "1033",
                            "",
                            new Version(1, 0, 0, 0));

            var lastVersion = keyword.LastUpdatedActivationKeywordModelVersion;

            var newVersion = new Version(lastVersion.Major, lastVersion.Minor, lastVersion.Build, lastVersion.Revision + 1);

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await keyword.UpdateKeyword(
            keyword.KeywordDisplayName,
            keyword.KeywordId,
            keyword.KeywordModelId,
            keyword.KeywordActivationModelDataFormat,
            keyword.KeywordActivationModelFilePath,
            newVersion,
            keyword.ConfirmationKeywordModelPath), "Invalid InputValues");
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
