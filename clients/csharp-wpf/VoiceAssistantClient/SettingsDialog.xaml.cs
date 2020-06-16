// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Win32;
    using VoiceAssistantClient.Settings;

    /// <summary>
    /// Interaction logic for SettingsDialog.xaml.
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private const int UrlHistoryMaxLength = 10;
        private readonly Dictionary<string, ConnectionProfile> connectionProfile;
        private RuntimeSettings settings;
        private bool renderComplete;

        public SettingsDialog(RuntimeSettings settings)
        {
            this.renderComplete = false;

            this.settings = settings;
            (
                this.ConnectionProfileName,
                this.connectionProfile,
                this.settings.Profile) = settings.Get();

            this.settings.Profile.CustomSpeechConfig = new CustomSpeechConfiguration(settings.CustomSpeechEndpointId);
            this.settings.Profile.VoiceDeploymentConfig = new VoiceDeploymentConfiguration(settings.VoiceDeploymentIds);
            this.settings.Profile.WakeWordConfig = new WakeWordConfiguration(settings.WakeWordPath);

            this.InitializeComponent();
            this.DataContext = this;
            this.Owner = App.Current.MainWindow;
        }

        public ConnectionProfile Profile { get; set; }

        public string ConnectionProfileName { get; set; }

        protected override void OnContentRendered(EventArgs e)
        {
            this.WakeWordPathTextBox.Text = this.settings.Profile.WakeWordPath ?? string.Empty;
            this.UpdateSaveButtonState();
            this.UpdateCustomSpeechStatus(false);
            this.UpdateVoiceDeploymentIdsStatus(false);
            this.UpdateWakeWordStatus();
            base.OnActivated(e);
            base.OnContentRendered(e);
            this.renderComplete = true;
        }

        protected override void OnActivated(EventArgs e)
        {
            this.ConnectionProfileComboBox.ItemsSource = this.settings.ConnectionProfileNameHistory;
            this.ConnectionProfileComboBox.Text = this.ConnectionProfileName;
            this.SubscriptionKeyTextBox.Text = this.settings.Profile.SubscriptionKey;
            this.SubscriptionRegionTextBox.Text = this.settings.Profile.SubscriptionKeyRegion;
            this.CustomCommandsAppIdTextBox.Text = this.settings.Profile.CustomCommandsAppId;
            this.BotIdTextBox.Text = this.settings.Profile.BotId;
            this.LanguageTextBox.Text = this.settings.Profile.ConnectionLanguage;
            this.LogFileTextBox.Text = this.settings.Profile.LogFilePath;
            this.UrlOverrideTextBox.Text = this.settings.Profile.UrlOverride;
            this.ProxyHost.Text = this.settings.Profile.ProxyHostName;
            this.ProxyPort.Text = this.settings.Profile.ProxyPortNumber;
            this.FromIdTextBox.Text = this.settings.Profile.FromId;
            this.WakeWordPathTextBox.Text = this.settings.Profile.WakeWordPath;
            this.WakeWordEnabledBox.IsChecked = this.settings.Profile.WakeWordEnabled;
            this.CustomSpeechEndpointIdTextBox.Text = this.settings.Profile.CustomSpeechEndpointId;
            this.CustomSpeechEnabledBox.IsChecked = this.settings.Profile.CustomSpeechEnabled;
            this.VoiceDeploymentIdsTextBox.Text = this.settings.Profile.VoiceDeploymentIds;
            this.VoiceDeploymentEnabledBox.IsChecked = this.settings.Profile.VoiceDeploymentEnabled;

            if (this.settings.ConnectionProfileNameHistory.Count == 1)
            {
                this.ConnectionProfileComboBox.Text = string.Empty;
            }

            base.OnActivated(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            this.ConnectionProfileName = this.ConnectionProfileComboBox.Text;
            this.SetProfileSettingsToConnectionSettingsTextBoxes();

            base.OnDeactivated(e);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(this.ConnectionProfileComboBox.Text))
            {
                if (this.connectionProfile.ContainsKey(this.ConnectionProfileComboBox.Text))
                {
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].SubscriptionKey = this.SubscriptionKeyTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].SubscriptionKeyRegion = this.SubscriptionRegionTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].CustomCommandsAppId = this.CustomCommandsAppIdTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].BotId = this.BotIdTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].ConnectionLanguage = this.LanguageTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].LogFilePath = this.LogFileTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].UrlOverride = this.UrlOverrideTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].ProxyHostName = this.ProxyHost.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].ProxyPortNumber = this.ProxyPort.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].FromId = this.FromIdTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].CustomSpeechEndpointId = this.CustomSpeechEndpointIdTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].CustomSpeechEnabled = (bool)this.CustomSpeechEnabledBox.IsChecked;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].VoiceDeploymentIds = this.VoiceDeploymentIdsTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].VoiceDeploymentEnabled = (bool)this.VoiceDeploymentEnabledBox.IsChecked;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].WakeWordPath = this.WakeWordPathTextBox.Text;
                    this.connectionProfile[this.ConnectionProfileComboBox.Text].WakeWordEnabled = (bool)this.WakeWordEnabledBox.IsChecked;

                    this.SetProfileSettingsToConnectionSettingsTextBoxes();
                }
                else
                {
                    this.connectionProfile.Add(this.ConnectionProfileName, new ConnectionProfile
                    {
                        SubscriptionKey = this.SubscriptionKeyTextBox.Text,
                        SubscriptionKeyRegion = this.SubscriptionRegionTextBox.Text,
                        CustomCommandsAppId = this.CustomCommandsAppIdTextBox.Text,
                        BotId = this.BotIdTextBox.Text,
                        ConnectionLanguage = this.LanguageTextBox.Text,
                        LogFilePath = this.LogFileTextBox.Text,
                        UrlOverride = this.UrlOverrideTextBox.Text,
                        ProxyHostName = this.ProxyHost.Text,
                        ProxyPortNumber = this.ProxyPort.Text,
                        FromId = this.FromIdTextBox.Text,
                        WakeWordPath = this.WakeWordPathTextBox.Text,
                        WakeWordEnabled = (bool)this.WakeWordEnabledBox.IsChecked,
                        CustomSpeechEndpointId = this.CustomSpeechEndpointIdTextBox.Text,
                        CustomSpeechEnabled = (bool)this.CustomSpeechEnabledBox.IsChecked,
                        VoiceDeploymentConfig = this.settings.Profile.VoiceDeploymentConfig,
                        VoiceDeploymentIds = this.VoiceDeploymentIdsTextBox.Text,
                        VoiceDeploymentEnabled = (bool)this.VoiceDeploymentEnabledBox.IsChecked,
                    });
                }

                this.AddConnectionProfileNameIntoHistory(this.ConnectionProfileName);
                this.AddConnectionProfileIntoHistory(this.connectionProfile);
                this.settings.Set(
                    this.ConnectionProfileName,
                    this.connectionProfile,
                    this.settings.Profile);
            }

            this.DialogResult = true;
            this.Close();
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(this.ConnectionProfileComboBox.Text))
            {
                this.connectionProfile.Remove(this.ConnectionProfileComboBox.Text);
                this.settings.ConnectionProfileNameHistory.Remove(this.ConnectionProfileComboBox.Text);
                if (this.settings.ConnectionProfileNameHistory.Count > 1)
                {
                    this.ConnectionProfileComboBox.Text = this.settings.ConnectionProfileNameHistory[1];
                }
                else
                {
                    this.ConnectionProfileComboBox.Text = string.Empty;
                    this.ConnectionProfileName = string.Empty;
                    this.SetConnectionSettingsTextBoxesToEmpty();
                }
            }
        }

        private void AddConnectionProfileNameIntoHistory(string connectionProfileName)
        {
            var profileNameHistory = this.settings.ConnectionProfileNameHistory;
            var existingItem = profileNameHistory.FirstOrDefault(item => string.Compare(item, connectionProfileName, StringComparison.OrdinalIgnoreCase) == 0);

            if (this.settings.ConnectionProfileNameHistory.Count > 0)
            {
                if (existingItem == null)
                {
                    profileNameHistory.Insert(1, connectionProfileName);
                    if (profileNameHistory.Count == UrlHistoryMaxLength)
                    {
                        profileNameHistory.RemoveAt(UrlHistoryMaxLength - 1);
                    }
                }
            }
            else
            {
                if (existingItem == null)
                {
                    profileNameHistory.Insert(0, " ");
                    profileNameHistory.Insert(1, connectionProfileName);
                    if (profileNameHistory.Count == UrlHistoryMaxLength)
                    {
                        profileNameHistory.RemoveAt(UrlHistoryMaxLength - 1);
                    }
                }
            }
        }

        private void AddConnectionProfileIntoHistory(Dictionary<string, ConnectionProfile> connectionProfile)
        {
            var connectionProfileHistory = this.settings.ConnectionProfileHistory;

            foreach (var item in connectionProfileHistory)
            {
                if (item != null)
                {
                    if (item.ContainsKey(this.ConnectionProfileComboBox.Text))
                    {
                        if (item == null)
                        {
                            connectionProfileHistory.Insert(0, connectionProfile);
                            if (connectionProfileHistory.Count == UrlHistoryMaxLength)
                            {
                                connectionProfileHistory.RemoveAt(UrlHistoryMaxLength - 1);
                            }
                        }
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.connectionProfile.ContainsKey(this.ConnectionProfileComboBox.Text))
            {
                this.SubscriptionKeyTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].SubscriptionKey;
                this.SubscriptionRegionTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].SubscriptionKeyRegion;
                this.CustomCommandsAppIdTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].CustomCommandsAppId;
                this.BotIdTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].BotId;
                this.LanguageTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].ConnectionLanguage;
                this.LogFileTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].LogFilePath;
                this.UrlOverrideTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].UrlOverride;
                this.ProxyHost.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].ProxyHostName;
                this.ProxyPort.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].ProxyPortNumber;
                this.FromIdTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].FromId;
                this.CustomSpeechEndpointIdTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].CustomSpeechEndpointId;
                this.CustomSpeechEnabledBox.IsChecked = this.connectionProfile[this.ConnectionProfileComboBox.Text].CustomSpeechEnabled;
                this.VoiceDeploymentIdsTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].VoiceDeploymentIds;
                this.VoiceDeploymentEnabledBox.IsChecked = this.connectionProfile[this.ConnectionProfileComboBox.Text].VoiceDeploymentEnabled;
                this.WakeWordPathTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].WakeWordPath;
                this.WakeWordEnabledBox.IsChecked = this.connectionProfile[this.ConnectionProfileComboBox.Text].WakeWordEnabled;
            }
            else if (!this.connectionProfile.ContainsKey(this.ConnectionProfileComboBox.Text) && this.settings.ConnectionProfileNameHistory.Count > 1)
            {
                this.SubscriptionKeyTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].SubscriptionKey;
                this.SubscriptionRegionTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].SubscriptionKeyRegion;
                this.CustomCommandsAppIdTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].CustomCommandsAppId;
                this.BotIdTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].BotId;
                this.LanguageTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].ConnectionLanguage;
                this.LogFileTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].LogFilePath;
                this.UrlOverrideTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].UrlOverride;
                this.ProxyHost.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].ProxyHostName;
                this.ProxyPort.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].ProxyPortNumber;
                this.FromIdTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].FromId;
                this.CustomSpeechEndpointIdTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].CustomSpeechEndpointId;
                this.CustomSpeechEnabledBox.IsChecked = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].CustomSpeechEnabled;
                this.VoiceDeploymentIdsTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].VoiceDeploymentIds;
                this.VoiceDeploymentEnabledBox.IsChecked = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].VoiceDeploymentEnabled;
                this.WakeWordPathTextBox.Text = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].WakeWordPath;
                this.WakeWordEnabledBox.IsChecked = this.connectionProfile[this.settings.ConnectionProfileNameHistory[1]].WakeWordEnabled;
            }
            else
            {
                this.ConnectionProfileComboBox.Text = string.Empty;
                this.settings.ConnectionProfileName = string.Empty;
                this.SetConnectionSettingsTextBoxesToEmpty();
            }

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

        private void UpdateSaveButtonState()
        {
            // BUGBUG: The transfer into variables does not seem to be done consistently with these events so we read straight from the controls
            var hasConnectionProfileName = !string.IsNullOrWhiteSpace(this.ConnectionProfileComboBox.Text) && this.ConnectionProfileComboBox.Text != " ";
            var hasSubscription = !string.IsNullOrWhiteSpace(this.SubscriptionKeyTextBox.Text);
            var hasRegion = !string.IsNullOrWhiteSpace(this.SubscriptionRegionTextBox.Text);
            var hasUrlOverride = !string.IsNullOrWhiteSpace(this.UrlOverrideTextBox.Text);

            var enableSaveButton = false;
            var enableDeleteButton = false;
            if (!hasConnectionProfileName)
            {
                this.SaveButtonInfoBlock.Text = "You must provide a profile name";
            }
            else if (!hasSubscription)
            {
                this.SaveButtonInfoBlock.Text = "You must provide a speech subscription key.";
            }
            else if (!hasRegion && !hasUrlOverride)
            {
                this.SaveButtonInfoBlock.Text = "You must provide a region or URL override.";
            }
            else if (hasRegion && hasUrlOverride)
            {
                this.SaveButtonInfoBlock.Text = "You must specify only region OR URL override, not both.";
            }
            else
            {
                this.SaveButtonInfoBlock.Text = string.Empty;
                enableSaveButton = true;
                enableDeleteButton = true;
            }

            this.SaveButton.IsEnabled = enableSaveButton;
            this.DeleteProfile.IsEnabled = enableDeleteButton;
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
            this.settings.Profile.CustomSpeechConfig = new CustomSpeechConfiguration(this.CustomSpeechEndpointIdTextBox.Text);

            if (!this.settings.Profile.CustomSpeechConfig.IsValid)
            {
                if (updateLabelOnInvalidContent)
                {
                    this.CustomSpeechEnabledBox.Content = "Invalid endpoint ID format";
                    Debug.WriteLine("Invalid endpoint ID format. It needs to be a GUID in the format ########-####-####-####-############");
                }
                else
                {
                    this.CustomSpeechEnabledBox.Content = "Click to enable";
                }

                this.settings.Profile.CustomSpeechEnabled = false;
                this.CustomSpeechEnabledBox.IsChecked = false;
            }
            else if (this.settings.Profile.CustomSpeechEnabled)
            {
                this.CustomSpeechEnabledBox.Content = "Custom speech will be used upon next connection";
            }
            else
            {
                this.CustomSpeechEnabledBox.Content = "Click to enable";
            }
        }

        private void CustomSpeechEndpointIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.settings.Profile.CustomSpeechEnabled = false;
                this.CustomSpeechEnabledBox.IsChecked = false;
            }

            this.CustomSpeechEnabledBox.Content = "Click to enable";
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
            this.settings.Profile.VoiceDeploymentConfig = new VoiceDeploymentConfiguration(this.VoiceDeploymentIdsTextBox.Text);

            if (!this.settings.Profile.VoiceDeploymentConfig.IsValid)
            {
                if (updateLabelOnInvalidContent)
                {
                    this.VoiceDeploymentEnabledBox.Content = "Invalid voice deployment IDs format";
                    Debug.WriteLine("Invalid voice deployment IDs format. It needs to be a GUID in the format ########-####-####-####-############");
                }
                else
                {
                    this.VoiceDeploymentEnabledBox.Content = "Click to enable";
                }

                this.settings.Profile.VoiceDeploymentEnabled = false;
                this.VoiceDeploymentEnabledBox.IsChecked = false;
            }
            else if (this.settings.Profile.VoiceDeploymentEnabled)
            {
                this.VoiceDeploymentEnabledBox.Content = "Voice deployment IDs will be used upon next connection";
            }
            else
            {
                this.VoiceDeploymentEnabledBox.Content = "Click to enable";
            }
        }

        private void VoiceDeploymentIdsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.settings.Profile.VoiceDeploymentEnabled = false;
                this.VoiceDeploymentEnabledBox.IsChecked = false;
            }

            this.VoiceDeploymentEnabledBox.Content = "Click to enable";
        }

        private void UpdateWakeWordStatus()
        {
            this.settings.Profile.WakeWordConfig = new WakeWordConfiguration(this.WakeWordPathTextBox.Text);

            if (!this.settings.Profile.WakeWordConfig.IsValid)
            {
                this.WakeWordEnabledBox.Content = "Invalid wake word model file or location";
                this.settings.Profile.WakeWordEnabled = false;
                this.WakeWordEnabledBox.IsChecked = false;
            }
            else if (this.settings.Profile.WakeWordEnabled)
            {
                this.WakeWordEnabledBox.Content = $"Will listen for the wake word upon next connection";
            }
            else
            {
                this.WakeWordEnabledBox.Content = "Click to enable";
            }
        }

        private void SubscriptionKeyRegionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateSaveButtonState();
        }

        private void SubscriptionKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateSaveButtonState();
        }

        private void CustomCommandsAppIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateSaveButtonState();
        }

        private void WakeWordPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.renderComplete)
            {
                this.settings.Profile.WakeWordEnabled = false;
                this.WakeWordEnabledBox.IsChecked = false;
            }

            this.WakeWordEnabledBox.Content = "Check to enable";
        }

        private void UrlOverrideTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateSaveButtonState();
        }

        private void ConnectionProfileTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.connectionProfile.Count == 0)
            {
                this.ConnectionProfileName = this.ConnectionProfileComboBox.Text;
            }

            if (this.connectionProfile.Count != 0)
            {
                if (this.connectionProfile.ContainsKey(this.ConnectionProfileComboBox.Text))
                {
                    this.SubscriptionKeyTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].SubscriptionKey;
                    this.SubscriptionRegionTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].SubscriptionKeyRegion;
                    this.CustomCommandsAppIdTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].CustomCommandsAppId;
                    this.BotIdTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].BotId;
                    this.LanguageTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].ConnectionLanguage;
                    this.LogFileTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].LogFilePath;
                    this.UrlOverrideTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].UrlOverride;
                    this.ProxyHost.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].ProxyHostName;
                    this.ProxyPort.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].ProxyPortNumber;
                    this.FromIdTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].FromId;
                    this.CustomSpeechEndpointIdTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].CustomSpeechEndpointId;
                    this.CustomSpeechEnabledBox.IsChecked = this.connectionProfile[this.ConnectionProfileComboBox.Text].CustomSpeechEnabled;
                    this.VoiceDeploymentIdsTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].VoiceDeploymentIds;
                    this.VoiceDeploymentEnabledBox.IsChecked = this.connectionProfile[this.ConnectionProfileComboBox.Text].VoiceDeploymentEnabled;
                    this.WakeWordPathTextBox.Text = this.connectionProfile[this.ConnectionProfileComboBox.Text].WakeWordPath;
                    this.WakeWordEnabledBox.IsChecked = this.connectionProfile[this.ConnectionProfileComboBox.Text].WakeWordEnabled;
                }
                else
                {
                    this.SetConnectionSettingsTextBoxesToEmpty();
                }
            }

            this.UpdateSaveButtonState();
        }

        private void SetConnectionSettingsTextBoxesToEmpty()
        {
            this.SubscriptionKeyTextBox.Text = string.Empty;
            this.SubscriptionRegionTextBox.Text = string.Empty;
            this.CustomCommandsAppIdTextBox.Text = string.Empty;
            this.BotIdTextBox.Text = string.Empty;
            this.LanguageTextBox.Text = string.Empty;
            this.LogFileTextBox.Text = string.Empty;
            this.UrlOverrideTextBox.Text = string.Empty;
            this.ProxyHost.Text = string.Empty;
            this.ProxyPort.Text = string.Empty;
            this.FromIdTextBox.Text = string.Empty;
            this.CustomSpeechEndpointIdTextBox.Text = string.Empty;
            this.CustomSpeechEnabledBox.IsChecked = false;
            this.VoiceDeploymentIdsTextBox.Text = string.Empty;
            this.VoiceDeploymentEnabledBox.IsChecked = false;
            this.WakeWordPathTextBox.Text = string.Empty;
            this.WakeWordEnabledBox.IsChecked = false;
        }

        private void SetProfileSettingsToConnectionSettingsTextBoxes()
        {
            this.settings.Profile.SubscriptionKey = this.SubscriptionKeyTextBox.Text;
            this.settings.Profile.SubscriptionKeyRegion = this.SubscriptionRegionTextBox.Text;
            this.settings.Profile.CustomCommandsAppId = this.CustomCommandsAppIdTextBox.Text;
            this.settings.Profile.BotId = this.BotIdTextBox.Text;
            this.settings.Profile.ConnectionLanguage = this.LanguageTextBox.Text;
            this.settings.Profile.LogFilePath = this.LogFileTextBox.Text;
            this.settings.Profile.UrlOverride = this.UrlOverrideTextBox.Text;
            this.settings.Profile.ProxyHostName = this.ProxyHost.Text;
            this.settings.Profile.ProxyPortNumber = this.ProxyPort.Text;
            this.settings.Profile.FromId = this.FromIdTextBox.Text;
            this.settings.Profile.CustomSpeechEndpointId = this.CustomSpeechEndpointIdTextBox.Text;
            this.settings.Profile.CustomSpeechEnabled = (bool)this.CustomSpeechEnabledBox.IsChecked;
            this.settings.Profile.VoiceDeploymentIds = this.VoiceDeploymentIdsTextBox.Text;
            this.settings.Profile.VoiceDeploymentEnabled = (bool)this.VoiceDeploymentEnabledBox.IsChecked;
            this.settings.Profile.WakeWordPath = this.WakeWordPathTextBox.Text;
            this.settings.Profile.WakeWordEnabled = (bool)this.WakeWordEnabledBox.IsChecked;
        }

        private void GitHubPageHyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}