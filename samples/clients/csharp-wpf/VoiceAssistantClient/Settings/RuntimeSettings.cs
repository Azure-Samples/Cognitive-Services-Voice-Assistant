// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace VoiceAssistantClient.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class RuntimeSettings : INotifyPropertyChanged
    {
        private ObservableCollection<string> cognitiveServiceKeyHistory = new ObservableCollection<string>();
        private ObservableCollection<string> cognitiveServiceRegionHistory = new ObservableCollection<string>();
        private ObservableCollection<string> customCommandsAppIdHistory = new ObservableCollection<string>();
        private string subscriptionKey;
        private string subscriptionKeyRegion;
        private string customCommandsAppId;
        private string language;
        private string logFilePath;
        private string customSpeechEndpointId;
        private bool customSpeechEnabled;
        private string voiceDeploymentIds;
        private bool voiceDeploymentEnabled;
        private string wakeWordPath;
        private bool wakeWordEnabled;
        private string urlOverride;
        private string proxyHostName;
        private string proxyPortNumber;
        private string fromId;

        public RuntimeSettings()
        {
            this.language = string.Empty;
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public string SubscriptionKey
        {
            get => this.subscriptionKey;
            set => this.SetProperty(ref this.subscriptionKey, value);
        }

        public string SubscriptionKeyRegion
        {
            get => this.subscriptionKeyRegion;
            set => this.SetProperty(ref this.subscriptionKeyRegion, value);
        }

        public string CustomCommandsAppId
        {
            get => this.customCommandsAppId;
            set => this.SetProperty(ref this.customCommandsAppId, value);
        }

        public string Language
        {
            get => this.language;
            set => this.SetProperty(ref this.language, value);
        }

        public string LogFilePath
        {
            get => this.logFilePath;
            set => this.SetProperty(ref this.logFilePath, value);
        }

        public string WakeWordPath
        {
            get => this.wakeWordPath;
            set => this.SetProperty(ref this.wakeWordPath, value);
        }

        public string CustomSpeechEndpointId
        {
            get => this.customSpeechEndpointId;
            set => this.SetProperty(ref this.customSpeechEndpointId, value);
        }

        public bool CustomSpeechEnabled
        {
            get => this.customSpeechEnabled;
            set => this.SetProperty(ref this.customSpeechEnabled, value);
        }

        public string VoiceDeploymentIds
        {
            get => this.voiceDeploymentIds;
            set => this.SetProperty(ref this.voiceDeploymentIds, value);
        }

        public bool VoiceDeploymentEnabled
        {
            get => this.voiceDeploymentEnabled;
            set => this.SetProperty(ref this.voiceDeploymentEnabled, value);
        }

        public bool WakeWordEnabled
        {
            get => this.wakeWordEnabled;
            set => this.SetProperty(ref this.wakeWordEnabled, value);
        }

        public string UrlOverride
        {
            get => this.urlOverride;
            set => this.SetProperty(ref this.urlOverride, value);
        }

        public string ProxyHostName
        {
            get => this.proxyHostName;
            set => this.SetProperty(ref this.proxyHostName, value);
        }

        public string ProxyPortNumber
        {
            get => this.proxyPortNumber;
            set => this.SetProperty(ref this.proxyPortNumber, value);
        }

        public string FromId
        {
            get => this.fromId;
            set => this.SetProperty(ref this.fromId, value);
        }

        public ObservableCollection<string> CognitiveServiceKeyHistory
        {
            get
            {
                return this.cognitiveServiceKeyHistory;
            }

            set
            {
                if (this.cognitiveServiceKeyHistory != null)
                {
                    this.cognitiveServiceKeyHistory.CollectionChanged -= this.CognitiveServiceKeyHistory_CollectionChanged;
                }

                this.cognitiveServiceKeyHistory = value;
                this.cognitiveServiceKeyHistory.CollectionChanged += this.CognitiveServiceKeyHistory_CollectionChanged;
                this.OnPropertyChanged();
            }
        }

        public ObservableCollection<string> CognitiveServiceRegionHistory
        {
            get
            {
                return this.cognitiveServiceRegionHistory;
            }

            set
            {
                if (this.cognitiveServiceRegionHistory != null)
                {
                    this.cognitiveServiceRegionHistory.CollectionChanged -= this.CognitiveServiceRegionHistory_CollectionChanged;
                }

                this.cognitiveServiceRegionHistory = value;
                this.cognitiveServiceRegionHistory.CollectionChanged += this.CognitiveServiceRegionHistory_CollectionChanged;
                this.OnPropertyChanged();
            }
        }

        public ObservableCollection<string> CustomCommandsAppIdHistory
        {
            get
            {
                return this.customCommandsAppIdHistory;
            }

            set
            {
                if (this.customCommandsAppIdHistory != null)
                {
                    this.customCommandsAppIdHistory.CollectionChanged -= this.CustomCommandsAppIdHistory_CollectionChanged;
                }

                this.customCommandsAppIdHistory = value;
                this.customCommandsAppIdHistory.CollectionChanged += this.CustomCommandsAppIdHistory_CollectionChanged;
                this.OnPropertyChanged();
            }
        }

        internal (string subscriptionKey, string subscriptionKeyRegion, string customCommandsAppId, string language, string logFilePath, string customSpeechEndpointId, bool customSpeechEnabled, string voiceDeploymentIds, bool voiceDeploymentEnabled, bool wakeWordEnabled, string urlOverride,
            string proxyHostName, string proxyPortNumber, string fromId, ObservableCollection<string> CognitiveServiceKeyHistory, ObservableCollection<string> CognitiveServiceRegionHistory) Get()
        {
            return (
                this.subscriptionKey,
                this.subscriptionKeyRegion,
                this.customCommandsAppId,
                this.language,
                this.logFilePath,
                this.customSpeechEndpointId,
                this.customSpeechEnabled,
                this.voiceDeploymentIds,
                this.voiceDeploymentEnabled,
                this.wakeWordEnabled,
                this.urlOverride,
                this.proxyHostName,
                this.proxyPortNumber,
                this.fromId,
                this.CognitiveServiceKeyHistory,
                this.CognitiveServiceRegionHistory);
        }

        internal void Set(
            string subscriptionKey,
            string subscriptionKeyRegion,
            string customCommandsAppId,
            string language,
            string logFilePath,
            string customSpeechEndpointId,
            bool customSpeechEnabled,
            string voiceDeploymentIds,
            bool voiceDeploymentEnabled,
            string wakeWordPath,
            bool wakeWordEnabled,
            string urlOverride,
            string proxyHostName,
            string proxyPortNumber,
            string fromId,
            ObservableCollection<string> cognitiveServiceKeyHistory,
            ObservableCollection<string> cognitiveServiceRegionHistory)
        {
            (this.subscriptionKey,
                this.subscriptionKeyRegion,
                this.customCommandsAppId,
                this.language,
                this.logFilePath,
                this.customSpeechEndpointId,
                this.customSpeechEnabled,
                this.voiceDeploymentIds,
                this.voiceDeploymentEnabled,
                this.wakeWordPath,
                this.wakeWordEnabled,
                this.urlOverride,
                this.proxyHostName,
                this.proxyPortNumber,
                this.fromId,
                this.cognitiveServiceKeyHistory,
                this.cognitiveServiceRegionHistory)
                =
            (subscriptionKey,
                subscriptionKeyRegion,
                customCommandsAppId,
                language,
                logFilePath,
                customSpeechEndpointId,
                customSpeechEnabled,
                voiceDeploymentIds,
                voiceDeploymentEnabled,
                wakeWordPath,
                wakeWordEnabled,
                urlOverride,
                proxyHostName,
                proxyPortNumber,
                fromId,
                CognitiveServiceKeyHistory,
                CognitiveServiceRegionHistory);
        }

        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (!object.Equals(storage, value))
            {
                storage = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CognitiveServiceKeyHistory_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(this.CognitiveServiceKeyHistory));
        }

        private void CognitiveServiceRegionHistory_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(this.CognitiveServiceRegionHistory));
        }

        private void CustomCommandsAppIdHistory_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(this.CustomCommandsAppIdHistory));
        }
    }
}
