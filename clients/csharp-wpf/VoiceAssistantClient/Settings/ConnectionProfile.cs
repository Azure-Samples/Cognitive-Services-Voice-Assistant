// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantClient.Settings
{
    public class ConnectionProfile
    {
        public string SubscriptionKey { get; set; }

        public string SubscriptionKeyRegion { get; set; }

        public string CustomCommandsAppId { get; set; }

        public string BotId { get; set; }

        public string ConnectionLanguage { get; set; }

        public string LogFilePath { get; set; }

        public string UrlOverride { get; set; }

        public string ProxyHostName { get; set; }

        public string ProxyPortNumber { get; set; }

        public string FromId { get; set; }

        public string WakeWordPath { get; set; }

        public WakeWordConfiguration WakeWordConfig { get; set; }

        public bool WakeWordEnabled { get; set; }

        public CustomSpeechConfiguration CustomSpeechConfig { get; set; }

        public string CustomSpeechEndpointId { get; set; }

        public bool CustomSpeechEnabled { get; set; }

        public VoiceDeploymentConfiguration VoiceDeploymentConfig { get; set; }

        public string VoiceDeploymentIds { get; set; }

        public bool VoiceDeploymentEnabled { get; set; }
    }
}
