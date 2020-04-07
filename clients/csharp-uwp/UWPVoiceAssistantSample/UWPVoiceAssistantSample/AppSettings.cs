﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Newtonsoft.Json;

    /// <summary>
    /// Bot specific application settings obtained for config.json.
    /// </summary>
    public class AppSettings
    {
        private static ILogProvider logger = LogRouter.GetClassLogger();

        /// <summary>
        /// Gets or sets Speech Subscription Key.
        /// </summary>
        public string SpeechSubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets Azure Region.
        /// </summary>
        public string AzureRegion { get; set; }

        /// <summary>
        /// Gets or sets CustomSpeechId.
        /// </summary>
        public string CustomSpeechId { get; set; }

        /// <summary>
        /// Gets or sets CustomVoiceId.
        /// </summary>
        public string CustomVoiceIds { get; set; }

        /// <summary>
        /// Gets or sets Custom Commands App Id.
        /// </summary>
        public string CustomCommandsAppId { get; set; }

        /// <summary>
        /// Gets or sets Bot Id.
        /// </summary>
        public string BotId { get; set; }

        /// <summary>
        /// Reads and deserializes the configuration file.
        /// </summary>
        /// <param name="configFile">config.json</param>
        /// <returns>Instance of AppSettings.</returns>
        public static AppSettings Load(string configFile)
        {
            StreamReader file = new StreamReader(configFile);
            string config = file.ReadToEnd();
            file.Close();
            AppSettings instance = JsonConvert.DeserializeObject<AppSettings>(config);
            ValidateAppSettings(instance);

            return instance;
        }

        /// <summary>
        /// Verifies if Speech Subscription Key is provided and parses it to a GUID.
        /// </summary>
        /// <param name="key">Speech Subscription Key.</param>
        /// <returns>Bool - true if speech key is a valid guid.</returns>
        public static bool ValidateSubscriptionKey(string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
            {
                string message = $"Failed to obtain Speech Subscription Key";
                logger.Log(message);
                throw new MissingFieldException(message);
            }

            if (Guid.TryParse(key, out Guid parsedGuid) &&
                parsedGuid.ToString("N", null).Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verifies if Azure Region provided is listed within the speechRegions.
        /// </summary>
        /// <param name="region">Azure Region</param>
        /// <returns>Bool - true if region is within the speechRegions.</returns>
        public static bool ValidateAzureRegion(string region)
        {
            List<string> speechRegions = new List<string>() { "westus", "westus2", "eastus", "eastus2", "westeurope", "northeurope", "southeastasia" };

            return speechRegions.Contains(region.ToLower(CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Verified if Custom Speech Id and Custom Voice Ids are provided and parses them to GUID's.
        /// </summary>
        /// <param name="id">Custom Speech Id or Custom Voice Id.</param>
        /// <returns>Bool - true if id is a valid Guid.</returns>
        public static bool ValidateCustomId(string id)
        {
            if (Guid.TryParse(id, out Guid parsedGuid))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validate AppSettings instance.
        /// </summary>
        /// <param name="instance">An instance of AppSettings.</param>
        public static void ValidateAppSettings(AppSettings instance)
        {
            if (ValidateSubscriptionKey(instance.SpeechSubscriptionKey) == false)
            {
                logger.Log("Failed to validate Speech Key");
            }

            if (string.IsNullOrWhiteSpace(instance.AzureRegion) ||
                ValidateAzureRegion(instance.AzureRegion) == false)
            {
                logger.Log("Failed to validate Azure Region");
            }

            if (!string.IsNullOrWhiteSpace(instance.CustomSpeechId) || !string.IsNullOrWhiteSpace(instance.CustomVoiceIds))
            {
                if (ValidateCustomId(instance.CustomSpeechId) == false)
                {
                    logger.Log("Failed to validate Custom Speech Id");
                }

                if (ValidateCustomId(instance.CustomVoiceIds) == false)
                {
                    logger.Log("Failed to validate Custom Voice Id");
                }
            }

            if (!string.IsNullOrWhiteSpace(instance.CustomCommandsAppId))
            {
                if (ValidateCustomId(instance.CustomCommandsAppId) == false)
                {
                    logger.Log("Failed to validate Custom Commands App Id");
                }
            }

            if (!string.IsNullOrWhiteSpace(instance.BotId))
            {
                if (ValidateCustomId(instance.BotId) == false)
                {
                    logger.Log("Failed to validate Bot Id");
                }
            }
        }
    }
}
