// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DLSpeechClient
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using DLSpeechClient.Settings;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for SettingsDialog.xaml.
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private const int UrlHistoryMaxLength = 10;
        private RuntimeSettings settings;

        private bool renderComplete;

        public SettingsDialog(RuntimeSettings settings)
        {
            this.renderComplete = false;

            this.settings = settings;
            (
                this.SubscriptionKey,
                this.SubscriptionKeyRegion,
                this.CustomCommandsAppId,
                this.ConnectionLanguage,
                this.LogFilePath,
                this.CustomSpeechEndpointId,
                this.CustomSpeechEnabled,
                this.VoiceDeploymentIds,
                this.VoiceDeploymentEnabled,
                this.WakeWordEnabled,
                this.UrlOverride,
                this.ProxyHostName,
                this.ProxyPortNumber,
                this.FromId,
                this.settings.CognitiveServiceKeyHistory,
                this.settings.CognitiveServiceRegionHistory) = settings.Get();

            this.CustomSpeechConfig = new CustomSpeechConfiguration(settings.CustomSpeechEndpointId);
            this.VoiceDeploymentConfig = new VoiceDeploymentConfiguration(settings.VoiceDeploymentIds);
            this.WakeWordConfig = new WakeWordConfiguration(settings.WakeWordPath);

            this.InitializeComponent();
            this.DataContext = this;
            this.Owner = App.Current.MainWindow;
        }

        public string SubscriptionKey { get; set; }

        public string SubscriptionKeyRegion { get; set; }

        public string CustomCommandsAppId { get; set; }

        public string ConnectionLanguage { get; set; }

        public string LogFilePath { get; set; }

        public string UrlOverride { get; set; }

        public string ProxyHostName { get; set; }

        public string ProxyPortNumber { get; set; }

        public string FromId { get; set; }

        public WakeWordConfiguration WakeWordConfig { get; set; }

        public bool WakeWordEnabled { get; set; }

        public CustomSpeechConfiguration CustomSpeechConfig { get; set; }

        public string CustomSpeechEndpointId { get; set; }

        public bool CustomSpeechEnabled { get; set; }

        public VoiceDeploymentConfiguration VoiceDeploymentConfig { get; set; }

        public string VoiceDeploymentIds { get; set; }

        public bool VoiceDeploymentEnabled { get; set; }

        protected override void OnContentRendered(EventArgs e)
        {
            this.WakeWordPathTextBox.Text = this.settings.WakeWordPath ?? string.Empty;
            this.UpdateOkButtonState();
            this.UpdateCustomSpeechStatus(false);
            this.UpdateVoiceDeploymentIdsStatus(false);
            this.UpdateWakeWordStatus();
            base.OnContentRendered(e);
            this.renderComplete = true;
        }

        protected override void OnActivated(EventArgs e)
        {
            this.SubscriptionKeyComboBox.ItemsSource = this.settings.CognitiveServiceKeyHistory;
            this.SubscriptionKeyComboBox.Text = this.SubscriptionKey;
            this.SubscriptionRegionComboBox.ItemsSource = this.settings.CognitiveServiceRegionHistory;
            this.SubscriptionRegionComboBox.Text = this.SubscriptionKeyRegion;
            this.CustomCommandsAppIdComboBox.ItemsSource = this.settings.CustomCommandsAppIdHistory;
            this.CustomCommandsAppIdComboBox.Text = this.CustomCommandsAppId;
            base.OnActivated(e);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.AddCognitiveServicesKeyEntryIntoHistory(this.SubscriptionKey);
            this.AddCognitiveServicesRegionEntryIntoHistory(this.SubscriptionKeyRegion);
            this.AddCustomCommandsAppIdEntryIntoHistory(this.CustomCommandsAppId);
            this.settings.Set(
                this.SubscriptionKey,
                this.SubscriptionKeyRegion,
                this.CustomCommandsAppId,
                this.ConnectionLanguage,
                this.LogFilePath,
                this.CustomSpeechEndpointId,
                this.CustomSpeechEnabled,
                this.VoiceDeploymentIds,
                this.VoiceDeploymentEnabled,
                this.WakeWordConfig.Path,
                this.WakeWordEnabled,
                this.UrlOverride,
                this.ProxyHostName,
                this.ProxyPortNumber,
                this.FromId,
                this.settings.CognitiveServiceKeyHistory,
                this.settings.CognitiveServiceRegionHistory);

            this.DialogResult = true;
            this.Close();
        }

        private void AddCognitiveServicesKeyEntryIntoHistory(string cognitiveServicesKey)
        {
            var keyHistory = this.settings.CognitiveServiceKeyHistory;

            var existingItem = keyHistory.FirstOrDefault(item => string.Compare(item, cognitiveServicesKey, StringComparison.OrdinalIgnoreCase) == 0);

            if (existingItem == null)
            {
                keyHistory.Insert(0, cognitiveServicesKey);
                if (keyHistory.Count == UrlHistoryMaxLength)
                {
                    keyHistory.RemoveAt(UrlHistoryMaxLength - 1);
                }
            }
        }

        private void AddCognitiveServicesRegionEntryIntoHistory(string cognitiveServicesKey)
        {
            var regionHistory = this.settings.CognitiveServiceRegionHistory;

            var existingItem = regionHistory.FirstOrDefault(item => string.Compare(item, cognitiveServicesKey, StringComparison.OrdinalIgnoreCase) == 0);

            if (existingItem == null)
            {
                regionHistory.Insert(0, cognitiveServicesKey);
                if (regionHistory.Count == UrlHistoryMaxLength)
                {
                    regionHistory.RemoveAt(UrlHistoryMaxLength - 1);
                }
            }
        }

        private void AddCustomCommandsAppIdEntryIntoHistory(string customCommandsAppId)
        {
            var idHistory = this.settings.CustomCommandsAppIdHistory;

            var existingItem = idHistory.FirstOrDefault(item => string.Compare(item, customCommandsAppId, StringComparison.OrdinalIgnoreCase) == 0);

            if (existingItem == null)
            {
                idHistory.Insert(0, customCommandsAppId);
                if (idHistory.Count == UrlHistoryMaxLength)
                {
                    idHistory.RemoveAt(UrlHistoryMaxLength - 1);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void WakeWordBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog();

            // Filter files by the file extension .table, to find the file downloaded
            // from the Azure web portal for "Speech Customization - Custom Wake Word"
            openDialog.Filter = "Wake word files (*.table)|*.table|All files (*.*)|*.*";

            try
            {
                var fileInfo = new FileInfo(this.WakeWordPathTextBox.Text);
                openDialog.InitialDirectory = fileInfo.DirectoryName;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                Debug.WriteLine($"Bad path for initial directory: {ex.Message}");
            }
#pragma warning restore CA1031 // Do not catch general exception types

            bool? result = openDialog.ShowDialog();

            if (result == true)
            {
                this.WakeWordPathTextBox.Text = openDialog.FileName;
            }
        }

        private void WakeWordEnabledBox_Checked(object sender, RoutedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.UpdateWakeWordStatus();
            }
        }

        private void UpdateOkButtonState()
        {
            // BUGBUG: The transfer into variables does not seem to be done consistently with these events so we read straight from the controls
            bool enableOkButton = !string.IsNullOrWhiteSpace(this.SubscriptionKeyComboBox.Text) &&
                            (!string.IsNullOrWhiteSpace(this.SubscriptionRegionComboBox.Text) || !string.IsNullOrWhiteSpace(this.UrlOverrideTextBox.Text));
            this.OkButton.IsEnabled = enableOkButton;
        }

        private void CustomSpeechEnabledBox_Checked(object sender, RoutedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.UpdateCustomSpeechStatus(true);
            }
        }

        private void UpdateCustomSpeechStatus(bool updateLabelOnInvalidContent)
        {
            this.CustomSpeechConfig = new CustomSpeechConfiguration(this.CustomSpeechEndpointIdTextBox.Text);

            if (!this.CustomSpeechConfig.IsValid)
            {
                if (updateLabelOnInvalidContent)
                {
                    this.CustomSpeechStatusLabel.Content = "Invalid endpoint ID format";
                    Debug.WriteLine("Invalid endpoint ID format. It needs to be a GUID in the format ########-####-####-####-############");
                }
                else
                {
                    this.CustomSpeechStatusLabel.Content = "Click to enable";
                }

                this.CustomSpeechEnabled = false;
                this.CustomSpeechEnabledBox.IsChecked = false;
            }
            else if (this.CustomSpeechEnabled)
            {
                this.CustomSpeechStatusLabel.Content = "Custom speech will be used upon next connection";
            }
            else
            {
                this.CustomSpeechStatusLabel.Content = "Click to enable";
            }
        }

        private void CustomSpeechEndpointIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.CustomSpeechEnabled = false;
                this.CustomSpeechEnabledBox.IsChecked = false;
            }

            this.CustomSpeechStatusLabel.Content = "Click to enable";
        }

        private void VoiceDeploymentEnabledBox_Checked(object sender, RoutedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.UpdateVoiceDeploymentIdsStatus(true);
            }
        }

        private void UpdateVoiceDeploymentIdsStatus(bool updateLabelOnInvalidContent)
        {
            this.VoiceDeploymentConfig = new VoiceDeploymentConfiguration(this.VoiceDeploymentIdsTextBox.Text);

            if (!this.VoiceDeploymentConfig.IsValid)
            {
                if (updateLabelOnInvalidContent)
                {
                    this.VoiceDeploymentStatusLabel.Content = "Invalid voice deployment IDs format";
                    Debug.WriteLine("Invalid voice deployment IDs format. It needs to be a GUID in the format ########-####-####-####-############");
                }
                else
                {
                    this.VoiceDeploymentStatusLabel.Content = "Click to enable";
                }

                this.VoiceDeploymentEnabled = false;
                this.VoiceDeploymentEnabledBox.IsChecked = false;
            }
            else if (this.VoiceDeploymentEnabled)
            {
                this.VoiceDeploymentStatusLabel.Content = "Voice deployment IDs will be used upon next connection";
            }
            else
            {
                this.VoiceDeploymentStatusLabel.Content = "Click to enable";
            }
        }

        private void VoiceDeploymentIdsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.VoiceDeploymentEnabled = false;
                this.VoiceDeploymentEnabledBox.IsChecked = false;
            }

            this.VoiceDeploymentStatusLabel.Content = "Click to enable";
        }

        private void UpdateWakeWordStatus()
        {
            this.WakeWordConfig = new WakeWordConfiguration(this.WakeWordPathTextBox.Text);

            if (!this.WakeWordConfig.IsValid)
            {
                this.WakeWordStatusLabel.Content = "Invalid wake word model file or location";
                this.WakeWordEnabled = false;
                this.WakeWordEnabledBox.IsChecked = false;
            }
            else if (this.WakeWordEnabled)
            {
                this.WakeWordStatusLabel.Content = $"Will listen for the wake word upon next connection";
            }
            else
            {
                this.WakeWordStatusLabel.Content = "Click to enable";
            }
        }

        private void SubscriptionKeyRegionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateOkButtonState();
        }

        private void SubscriptionKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateOkButtonState();
        }

        private void CustomCommandsAppIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateOkButtonState();
        }

        private void WakeWordPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.WakeWordEnabled = false;
                this.WakeWordEnabledBox.IsChecked = false;
            }

            this.WakeWordStatusLabel.Content = "Check to enable";
        }
    }
}
