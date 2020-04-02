// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantClient.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    [Serializable]
    public class RuntimeSettings : INotifyPropertyChanged
    {
        private ObservableCollection<string> connectionProfileNameHistory = new ObservableCollection<string>();
        private ObservableCollection<Dictionary<string, ConnectionProfile>> connectionProfileHistory = new ObservableCollection<Dictionary<string, ConnectionProfile>>();
        private ObservableCollection<string> cognitiveServiceKeyHistory = new ObservableCollection<string>();
        private ObservableCollection<string> cognitiveServiceRegionHistory = new ObservableCollection<string>();
        private ObservableCollection<string> customCommandsAppIdHistory = new ObservableCollection<string>();
        private string connectionProfileName;
        private Dictionary<string, ConnectionProfile> connectionProfile = new Dictionary<string, ConnectionProfile>();
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

        public string ConnectionProfileName
        {
            get => this.connectionProfileName;
            set => this.SetProperty(ref this.connectionProfileName, value);
        }

        public Dictionary<string, ConnectionProfile> ConnectionProfile
        {
            get => this.connectionProfile;
            set => this.SetProperty(ref this.connectionProfile, value);
        }

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

        public ObservableCollection<string> ConnectionProfileNameHistory
        {
            get
            {
                return this.connectionProfileNameHistory;
            }

            set
            {
                if (this.connectionProfileNameHistory != null)
                {
                    this.connectionProfileNameHistory.CollectionChanged -= this.ConnectionProfileNameHistory_CollectionChanged;
                }

                this.connectionProfileNameHistory = value;
                this.connectionProfileNameHistory.CollectionChanged += this.ConnectionProfileNameHistory_CollectionChanged;
                this.OnPropertyChanged();
            }
        }

        public ObservableCollection<Dictionary<string, ConnectionProfile>> ConnectionProfileHistory
        {
            get
            {
                return this.connectionProfileHistory;
            }

            set
            {
                if (this.connectionProfileHistory != null)
                {
                    this.connectionProfileHistory.CollectionChanged -= this.ConnectionProfileHistory_CollectionChanged;
                }

                this.connectionProfileHistory = value;
                this.connectionProfileHistory.CollectionChanged += this.ConnectionProfileHistory_CollectionChanged;
                this.OnPropertyChanged();
            }
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

        internal (string connectionProfileName, Dictionary<string, ConnectionProfile> connectionProfile, ObservableCollection<string> ConnectionProfileNameHistory, ObservableCollection<Dictionary<string, ConnectionProfile>> ConnectionProfileHistory, ObservableCollection<string> CognitiveServiceKeyHistory, ObservableCollection<string> CognitiveServiceRegionHistory) Get()
        {
            return (
                this.connectionProfileName,
                this.connectionProfile,
                this.ConnectionProfileNameHistory,
                this.ConnectionProfileHistory,
                this.CognitiveServiceKeyHistory,
                this.CognitiveServiceRegionHistory);
        }

        internal void Set(
            string connectionProfileName,
            Dictionary<string, ConnectionProfile> connectionProfile,
            ObservableCollection<string> connectionProfileNameHistory,
            ObservableCollection<Dictionary<string, ConnectionProfile>> connectionProfileHistory,
            ObservableCollection<string> cognitiveServiceKeyHistory,
            ObservableCollection<string> cognitiveServiceRegionHistory)
        {
            (this.connectionProfileName,
                this.connectionProfile,
                this.connectionProfileNameHistory,
                this.connectionProfileHistory,
                this.cognitiveServiceKeyHistory,
                this.cognitiveServiceRegionHistory)
                =
            (connectionProfileName,
                connectionProfile,
                this.ConnectionProfileNameHistory,
                this.ConnectionProfileHistory,
                this.CognitiveServiceKeyHistory,
                this.CognitiveServiceRegionHistory);
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

        private void ConnectionProfileNameHistory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(this.ConnectionProfileNameHistory));
        }

        private void ConnectionProfileHistory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(this.ConnectionProfileHistory));
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
