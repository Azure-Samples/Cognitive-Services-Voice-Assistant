// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DLSpeechClient
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using DLSpeechClient.Settings;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Interaction logic for CustomActivity.xaml.
    /// </summary>
    public partial class CustomActivityWindow : Window
    {
        // Binding for the combobox
        private ObservableCollection<CustomActiviyJsonDataEntry> customPayloadDataCollection;

        public CustomActivityWindow(
                    ObservableCollection<CustomActiviyJsonDataEntry> activityCollection,
                    int selectedEntry)
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Owner = App.Current.MainWindow;

            this.customPayloadDataCollection = activityCollection;
            if (selectedEntry != -1)
            {
                var activityInfoEntry = this.customPayloadDataCollection[selectedEntry];
                this.CustomActivityTag = activityInfoEntry.Name;
                this.CustomActivityContent = activityInfoEntry.JsonData;
            }
            else
            {
                this.CustomActivityTag = string.Empty;
                this.CustomActivityContent = string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets a custom Bot-Framework Activity (bound to SendActivityPayload control).
        /// </summary>
        public string CustomActivityContent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tag (the name) of the custom Bot-Framework Activity.
        /// </summary>
        public string CustomActivityTag { get; set; }

        private void SaveActivityButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(this.CustomActivityTag) &&
                    !string.IsNullOrWhiteSpace(this.CustomActivityContent))
                {
                    // Chceck is valid jason
                    JObject.Parse(this.CustomActivityContent);

                    var activityInfoEntry = this.customPayloadDataCollection.FirstOrDefault(
                                                   (item) => item.Name.Equals(this.CustomActivityTag, StringComparison.OrdinalIgnoreCase));

                    // BUGBUG: no confirmation
                    if (activityInfoEntry != null)
                    {
                        activityInfoEntry.JsonData = this.CustomActivityContent;
                    }
                    else
                    {
                        activityInfoEntry = new CustomActiviyJsonDataEntry()
                        {
                            Name = this.CustomActivityTag,
                            JsonData = this.CustomActivityContent,
                        };
                        this.customPayloadDataCollection.Insert(0, activityInfoEntry);
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                MessageBox.Show("Invalid Json" + Environment.NewLine + ex.Message);
                return;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(this.CustomActivityTag))
            {
                var activityInfoEntry = this.customPayloadDataCollection.FirstOrDefault(
                                               (item) => item.Name.Equals(this.CustomActivityTag, StringComparison.OrdinalIgnoreCase));

                if (activityInfoEntry != null)
                {
                    this.customPayloadDataCollection.Remove(activityInfoEntry);
                }

                this.CustomActivityTag = string.Empty;

                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
