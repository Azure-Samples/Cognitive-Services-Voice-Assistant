// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using UWPVoiceAssistantSample.AudioCommon;
    using UWPVoiceAssistantSample.AudioInput;
    using Windows.ApplicationModel.ConversationalAgent;
    using Windows.Security.Authorization.AppCapabilityAccess;
    using Windows.Storage;
    using Windows.System;
    using Windows.System.Power;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Documents;
    using Windows.UI.Xaml.Media;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Collection of utterances from user and bot.
        /// </summary>
        public ObservableCollection<Conversation> Conversations;
        private readonly ServiceProvider services;
        private readonly ILogProvider logger;
        private readonly IKeywordRegistration keywordRegistration;
        private readonly IDialogManager dialogManager;
        private readonly IAgentSessionManager agentSessionManager;
        private App app;
        private int bufferIndex;
        private bool configModified;
        private bool hypotheizedSpeechToggle;
        private Conversation activeConversation;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.logger = LogRouter.GetClassLogger();

            this.InitializeComponent();

            this.app = App.Current as App;
            this.app.HasReachedForeground = true;

            this.services = this.app.Services;
            this.dialogManager = this.services.GetRequiredService<IDialogManager>();
            this.keywordRegistration = this.services.GetRequiredService<IKeywordRegistration>();
            this.agentSessionManager = this.services.GetRequiredService<IAgentSessionManager>();

            // Ensure that we restore the full view (not the compact mode) upon foreground launch
            _ = this.UpdateViewStateAsync();

            // Ensure we have microphone permissions and that we pop a consent dialog if the user
            // hasn't already given an explicit yes/no.
            _ = Task.Run(async () =>
            {
                var control = await AudioCaptureControl.GetInstanceAsync();
                await control.MicrophoneCapability.RequestAccessAsync();
            });

            // Kick off the registration and/or retrieval of the 1st-stage keyword information
            _ = this.DoKeywordSetupAsync();

            // Populate the drop-down list for TTS audio output formats and select the current choice
            var supportedFormats = DirectLineSpeechAudio.SupportedOutputFormats;
            foreach (var entry in supportedFormats)
            {
                this.OutputFormatComboBox.Items.Add(entry.Label);
            }

            this.OutputFormatComboBox.SelectedItem = this.OutputFormatComboBox.Items.FirstOrDefault(item =>
                item.ToString() == LocalSettingsHelper.OutputFormat.Label);

            // Wire a few pieces of UI handling that aren't trivially handled by XAML bindings
            this.AddUIHandlersAsync();

            // Ensure consistency between a few dependent controls and their settings
            this.UpdateUIBasedOnToggles();

            this.Conversations = new ObservableCollection<Conversation>();

            this.ChatHistoryListView.ContainerContentChanging += this.OnChatHistoryListViewContainerChanging;
        }

        private bool BackgroundTaskRegistered
        {
            get => MVARegistrationHelpers.IsBackgroundTaskRegistered;
            set
            {
                MVARegistrationHelpers.IsBackgroundTaskRegistered = value;
                _ = this.UpdateUIForSharedStateAsync();
            }
        }

        private void AddUIHandlersAsync()
        {
            this.AddSystemAvailabilityHandlers();
            this.AddDialogHandlersAsync();

            this.DismissButton.Click += (_, __) =>
            {
                WindowService.CloseWindow();
            };
            this.MicrophoneButton.Click += async (_, __) =>
            {
                this.dialogManager.HandleSignalDetection(DetectionOrigin.FromPushToTalk);
                await this.UpdateUIForSharedStateAsync();
            };
            this.ResetButton.Click += async (_, __) =>
            {
                await this.dialogManager.FinishConversationAsync();
                await this.dialogManager.StopAudioPlaybackAsync();
                this.Conversations.Clear();
                this.RefreshStatus();
            };
            this.OpenLogLocationButton.Click += async (_, __)
                => await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
        }

        private async Task UpdateViewStateAsync()
        {
            var session = await this.agentSessionManager.GetSessionAsync();
            if (session.IsUserAuthenticated)
            {
                var appView = ApplicationView.GetForCurrentView();
                await appView.TryEnterViewModeAsync(ApplicationViewMode.Default);
            }
        }

        private async Task DoKeywordSetupAsync()
        {
            var keywordConfig = await this.keywordRegistration.GetOrCreateKeywordConfigurationAsync();
            keywordConfig.AvailabilityChanged += async (s, e)
                => await this.UpdateUIForSharedStateAsync();
            await this.UpdateUIForSharedStateAsync();
        }

        private async void AddSystemAvailabilityHandlers()
        {
            var inputControl = await AudioCaptureControl.GetInstanceAsync();
            inputControl.MicrophoneCapability.AccessChanged += async (s, e)
                => await this.UpdateUIForSharedStateAsync();
            inputControl.AudioInputDeviceChanged += async ()
                => await this.UpdateUIForSharedStateAsync();
            inputControl.InputVolumeStateChanged += async ()
                => await this.UpdateUIForSharedStateAsync();
            var session = await this.agentSessionManager.GetSessionAsync();
            if (session != null)
            {
                session.SystemStateChanged += async (s, e)
                    => await this.UpdateUIForSharedStateAsync();
            }

            PowerManager.EnergySaverStatusChanged += async (s, e)
                => await this.UpdateUIForSharedStateAsync();
        }

        private void AddDialogHandlersAsync()
        {
            // Ensure we update UI state (like buttons) when detection begins
            this.dialogManager.DialogStateChanged += async (s, e)
                => await this.UpdateUIForSharedStateAsync();

            // TODO: This is probably too busy for hypothesis events; better way of showing intermediate results?
            this.dialogManager.SpeechRecognizing += (s, e) =>
            {
                this.AddHypothesizedSpeechToTextBox(e);
            };
            this.dialogManager.SpeechRecognized += (s, e) =>
            {
                this.AddMessageToStatus(e);
            };
            this.dialogManager.DialogResponseReceived += (s, e) =>
            {
                // TODO: Duplicate wrapper creation unnecessary
                var wrapper = new ActivityWrapper(e.MessageBody.ToString());
                if (wrapper.Type == ActivityWrapper.ActivityType.Message)
                {
                    this.AddBotResponse(wrapper.Message);
                }
            };

            this.logger.LogAvailable += (s, e) =>
            {
                this.ReadLogBuffer();
            };
            this.logger.Log(LogMessageLevel.Noise, "Main page created, UI rendering");
        }

        private void UpdateUIBasedOnToggles()
        {
            var useSpeechSdk = LocalSettingsHelper.EnableSpeechSDK;
            var visibility = useSpeechSdk ? Visibility.Visible : Visibility.Collapsed;
            var useKws = useSpeechSdk && LocalSettingsHelper.EnableSecondStageKws;
            var enableLogs = useSpeechSdk && LocalSettingsHelper.EnableSdkLogging;

            this.EnableSpeechSDKLoggingToggle.Visibility = visibility;
            this.EnableSecondStageKwsToggle.Visibility = visibility;
            this.EnableSecondStageKwsToggle.IsOn = useKws;
            this.EnableSpeechSDKLoggingToggle.IsOn = enableLogs;

            this.RefreshStatus();
        }

        private async Task UpdateUIForSharedStateAsync()
        {
            // UI changes must be performed on the UI thread.
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                this.RefreshStatus();

                var session = await this.agentSessionManager.GetSessionAsync();
                var audioControl = await AudioCaptureControl.GetInstanceAsync();
                var micStatus = audioControl.MicrophoneCapability.CheckAccess();

                var agentIdle = session == null || session.AgentState == ConversationalAgentState.Inactive;
                var micReady = micStatus == AppCapabilityAccessStatus.Allowed && audioControl.HasAudioInputAvailable;

                var keywordConfig = await this.keywordRegistration.GetOrCreateKeywordConfigurationAsync();

                this.AppVoiceActivationEnabledToggle.IsEnabled = keywordConfig != null;
                this.AppVoiceActivationEnabledToggle.OffContent = keywordConfig != null
                    ? "Application has disabled voice activation."
                    : "App voice activation status unknown: configuration not yet queried";
                this.AppVoiceActivationEnabledToggle.IsOn = keywordConfig != null && keywordConfig.AvailabilityInfo.IsEnabled;

                this.MicrophoneButton.IsEnabled = agentIdle && micReady;
                this.MicrophoneButton.Content = micReady ? Glyphs.Microphone : Glyphs.MicrophoneOff;

                var microphoneStatusInfo = await UIAudioStatus.GetMicrophoneStatusAsync();
                this.MicrophoneInfoIcon.Glyph = microphoneStatusInfo.Glyph;
                this.MicrophoneInfoIcon.Foreground = new SolidColorBrush(microphoneStatusInfo.Color);
                this.MicrophoneLinkButton.Content = microphoneStatusInfo.Status;

                var voiceActivationStatusInfo = await UIAudioStatus.GetVoiceActivationStatusAsync();
                this.VAStatusIcon.Glyph = voiceActivationStatusInfo.Glyph;
                this.VAStatusIcon.Foreground = new SolidColorBrush(voiceActivationStatusInfo.Color);
                this.VoiceActivationLinkButton.Content = voiceActivationStatusInfo.Status;

                this.DismissButton.Visibility = session.IsUserAuthenticated ? Visibility.Collapsed : Visibility.Visible;

                //if (!this.BackgroundTaskRegistered && !micReady)
                //{
                //    ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = 1560, Height = 800 });
                //}

                //if (!this.BackgroundTaskRegistered && micReady)
                //{
                //    ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size { Width = ((int)this.MainPageWindow.ActualWidth - 30), Height = 800 });
                //    ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = ((int)this.MainPageWindow.ActualWidth - 30) , Height = 800 });
                //}

                //if (this.BackgroundTaskRegistered && !micReady)
                //{
                //    ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size { Width = ((int)this.ControlsGrid.ActualWidth) + ((int)this.ChatGrid.ActualWidth) + ((int)this.LogGrid.ActualWidth), Height = 800 });
                //}

                //if (this.BackgroundTaskRegistered && micReady)
                //{
                //    ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size { Width = ((int)this.MainPageWindow.ActualWidth - 30), Height = 800 });
                //    //ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = ((int)this.MainPageWindow.ActualWidth - 30) + ((int)this.LogGrid.ActualWidth), Height = 800 });
                //    ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = ((int)this.MainPageWindow.ActualWidth - 30), Height = 800 });
                //}

            });
        }

        private async void RefreshStatus()
        {
            var session = await this.agentSessionManager.GetSessionAsync().ConfigureAwait(false);
            var agentStatusMessage = session == null ?
               "No current agent session"
               : $"{session.AgentState.ToString()} {(this.app.InvokedViaSignal ? "[via signal]" : string.Empty)}";

            _ = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.ConversationStateTextBlock.Text = $"System: {agentStatusMessage}";
            });
        }

        private void AddBotResponse(string message)
        {
            _ = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.Conversations.Add(new Conversation
                {
                    Body = message,
                    Time = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                    Sent = false,
                });
            });
        }

        private void AddHypothesizedSpeechToTextBox(string message)
        {
            _ = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (this.hypotheizedSpeechToggle)
                {
                    if (this.activeConversation == null)
                    {
                        this.activeConversation = new Conversation
                        {
                            Body = message,
                            Time = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                            Sent = true,
                        };

                        this.Conversations.Add(this.activeConversation);
                    }

                    this.activeConversation.Body = message;

                    this.Conversations.Last().Body = message;
                }
            });
        }

        private void AddMessageToStatus(string message)
        {
            _ = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (this.activeConversation == null)
                {
                    this.activeConversation = new Conversation
                    {
                        Body = message,
                        Time = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                        Sent = true,
                    };

                    this.Conversations.Add(this.activeConversation);
                }

                this.activeConversation.Body = message;
                this.activeConversation = null;
            });

            this.RefreshStatus();
        }

        private async void ReadLogBuffer()
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                for (var i = 0; i < this.logger.LogBuffer.Count; i++)
                {
                    if (this.bufferIndex < this.logger.LogBuffer.Count)
                    {
                        string text = this.logger.LogBuffer[this.bufferIndex];
                        if (text.Contains(" : ", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] split = text.Split(" : ");
                            Paragraph paragraph = new Paragraph();
                            Run run = new Run();
                            run.Text = split[1];
                            paragraph.Inlines.Add(run);
                            this.ChangeLogTextBlock.Blocks.Add(paragraph);
                        }
                        else if (text.Contains("Information", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] split = text.Split("Information");
                            Paragraph paragraph = new Paragraph();
                            Run run = new Run();
                            run.Text = split[1];
                            paragraph.Inlines.Add(run);
                            paragraph.Foreground = new SolidColorBrush(Colors.Blue);
                            this.ChangeLogTextBlock.Blocks.Add(paragraph);
                        }
                        else if (text.Contains("Error", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] split = text.Split("Error");
                            Paragraph paragraph = new Paragraph();
                            Run run = new Run();
                            run.Text = split[1];
                            paragraph.Inlines.Add(run);
                            paragraph.Foreground = new SolidColorBrush(Colors.Red);
                            this.ChangeLogTextBlock.Blocks.Add(paragraph);
                        }
                        else if (text.Contains("Noise", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] split = text.Split("Noise");
                            Paragraph paragraph = new Paragraph();
                            Run run = new Run();
                            run.Text = split[1];
                            paragraph.Inlines.Add(run);
                            paragraph.Foreground = new SolidColorBrush(Colors.Gray);
                            this.ChangeLogTextBlock.Blocks.Add(paragraph);
                        }
                        else
                        {
                            Paragraph paragraph = new Paragraph();
                            Run run = new Run();
                            run.Text = text;
                            paragraph.Inlines.Add(run);
                            this.ChangeLogTextBlock.Blocks.Add(paragraph);
                        }

                        this.bufferIndex++;
                    }
                }

                this.ChangeLogScrollViewer.ChangeView(0.0f, double.MaxValue, 1.0f);
            });
        }

        private async void OpenConfigClick(object o, RoutedEventArgs e)
        {
            _ = o;
            _ = e;

            // Add FileSystemWatcher to watch config file. If changed set configmodified to true.
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = Directory.GetCurrentDirectory();
                watcher.NotifyFilter = NotifyFilters.LastWrite;

                watcher.Filter = "*.json";

                string fileName = "config.json";
                var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(fileName);

                if (file.Path != null)
                {
                    await Launcher.LaunchFileAsync(file);
                    this.logger.Log("Config file opened");
                    this.logger.Log("Click Load Config to use modified values");
                }

                this.configModified = true;
            }
        }

        private async void LoadConfigClick(object sender, RoutedEventArgs e)
        {
            var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///config.json"));

            AppSettings appSettings = AppSettings.Load(configFile.Path);

            var speechKeyModified = LocalSettingsHelper.SpeechSubscriptionKey != appSettings.SpeechSubscriptionKey;
            var speechRegionModified = LocalSettingsHelper.AzureRegion != appSettings.AzureRegion;
            var customSpeechIdModified = LocalSettingsHelper.CustomSpeechId != appSettings.CustomSpeechId;
            var customVoiceIdModified = LocalSettingsHelper.CustomVoiceIds != appSettings.CustomVoiceIds;
            var customCommandsAppIdModified = LocalSettingsHelper.CustomCommandsAppId != appSettings.CustomCommandsAppId;
            var botIdModified = LocalSettingsHelper.BotId != appSettings.BotId;

            this.configModified = speechKeyModified || speechRegionModified || customSpeechIdModified || customVoiceIdModified || customCommandsAppIdModified || botIdModified;

            if (this.configModified)
            {
                this.logger.Log("Configuration file has been modified");

                if (speechKeyModified)
                {
                    LocalSettingsHelper.SpeechSubscriptionKey = appSettings.SpeechSubscriptionKey;
                    this.logger.Log($"Speech Key: {LocalSettingsHelper.SpeechSubscriptionKey}");
                }

                if (speechRegionModified)
                {
                    LocalSettingsHelper.AzureRegion = appSettings.AzureRegion;
                    this.logger.Log($"Azure Region: {LocalSettingsHelper.AzureRegion}");
                }

                if (customSpeechIdModified)
                {
                    LocalSettingsHelper.CustomSpeechId = appSettings.CustomSpeechId;
                    this.logger.Log($"Custom Speech Id: {LocalSettingsHelper.CustomSpeechId}");
                }

                if (customVoiceIdModified)
                {
                    LocalSettingsHelper.CustomVoiceIds = appSettings.CustomVoiceIds;
                    this.logger.Log($"Custom Voice Id: {LocalSettingsHelper.CustomVoiceIds}");
                }

                if (customCommandsAppIdModified)
                {
                    LocalSettingsHelper.CustomCommandsAppId = appSettings.CustomCommandsAppId;
                    this.logger.Log($"Custom Commands App Id: {LocalSettingsHelper.CustomCommandsAppId}");
                }

                if (botIdModified)
                {
                    LocalSettingsHelper.BotId = appSettings.BotId;
                    this.logger.Log($"Bot Id: {LocalSettingsHelper.BotId}");
                }
            }
            else
            {
                this.logger.Log("No changes in config");
            }
        }

        private void CollapseControls(object sender, RoutedEventArgs e)
        {
            this.WindowsContolFlyoutItem.IsChecked = false;
            this.ToggleControls(sender, e);
        }

        private void CollapseLogs(object sender, RoutedEventArgs e)
        {
            this.WindowsLogFlyoutItem.IsChecked = false;
            this.ToggleControls(sender, e);
        }

        private void ToggleControls(object sender, RoutedEventArgs e)
        {
            if (this.WindowsContolFlyoutItem.IsChecked && this.WindowsLogFlyoutItem.IsChecked && !this.WindowsChatFlyoutItem.IsChecked)
            {
                this.ControlsGrid.Visibility = Visibility.Visible;
                this.LogGrid.Visibility = Visibility.Visible;
                this.ChatGrid.Visibility = Visibility.Collapsed;
                var logGridMargin = this.LogGrid.Margin;
                logGridMargin.Top = 0;
                this.LogGrid.Margin = logGridMargin;
                var controlsGridMargin = this.ControlsGrid.Margin;
                controlsGridMargin.Top = 0;
                this.ControlsGrid.Margin = controlsGridMargin;
                Grid.SetColumn(this.ControlsGrid, 0);
                Grid.SetColumn(this.LogGrid, 1);
                Grid.SetRow(this.LogGrid, 2);
                Grid.SetColumn(this.ChatGrid, 2);
                Grid.SetRow(this.ChatGrid, 0);
                Grid.SetColumn(this.HelpButtonGrid, 1);
                Grid.SetColumnSpan(this.ApplicationStateGrid, 2);
                Grid.SetColumn(this.HelpButtonGrid, 1);
                Grid.SetRowSpan(this.ApplicationStateGrid, 1);
                Grid.SetRow(this.VoiceSettingsStackPanel, 0);
                Grid.SetColumn(this.VoiceSettingsStackPanel, 0);
                Grid.SetRow(this.MicrophoneSettingsStackPanel, 0);
                Grid.SetColumn(this.MicrophoneSettingsStackPanel, 1);
                Grid.SetRow(this.ConversationStateStackPanel, 0);
                Grid.SetColumn(this.ConversationStateStackPanel, 2);
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size { Width = ((int)this.ControlsGrid.ActualWidth) + ((int)this.LogGrid.ActualWidth), Height = 800 });
                ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = ((int)this.ControlsGrid.ActualWidth) + ((int)this.LogGrid.ActualWidth), Height = 800 });
            }

            if (this.WindowsContolFlyoutItem.IsChecked && !this.WindowsLogFlyoutItem.IsChecked && this.WindowsChatFlyoutItem.IsChecked)
            {
                this.ControlsGrid.Visibility = Visibility.Visible;
                this.LogGrid.Visibility = Visibility.Collapsed;
                this.ChatGrid.Visibility = Visibility.Visible;
                Grid.SetColumn(this.ControlsGrid, 0);
                Grid.SetRow(this.ControlsGrid, 2);
                Grid.SetColumn(this.ChatGrid, 1);
                Grid.SetRow(this.ChatGrid, 2);
                var margin = this.ChatGrid.Margin;
                margin.Top = 0;
                this.ChatGrid.Margin = margin;
                var controlsGridMargin = this.ControlsGrid.Margin;
                controlsGridMargin.Top = 0;
                this.ControlsGrid.Margin = controlsGridMargin;
                Grid.SetColumnSpan(this.ApplicationStateGrid, 2);
                Grid.SetRowSpan(this.ApplicationStateGrid, 1);
                Grid.SetRow(this.VoiceSettingsStackPanel, 0);
                Grid.SetColumn(this.VoiceSettingsStackPanel, 0);
                Grid.SetRow(this.MicrophoneSettingsStackPanel, 0);
                Grid.SetColumn(this.MicrophoneSettingsStackPanel, 1);
                Grid.SetRow(this.ConversationStateStackPanel, 0);
                Grid.SetColumn(this.ConversationStateStackPanel, 2);
                Grid.SetColumn(this.HelpButtonGrid, 1);
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size { Width = ((int)this.ControlsGrid.ActualWidth) + ((int)this.ChatGrid.ActualWidth), Height = 800 });
                ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = ((int)this.ControlsGrid.ActualWidth) + ((int)this.ChatGrid.ActualWidth), Height = 800 });
            }

            if (!this.WindowsContolFlyoutItem.IsChecked && this.WindowsLogFlyoutItem.IsChecked && this.WindowsChatFlyoutItem.IsChecked)
            {
                this.ControlsGrid.Visibility = Visibility.Collapsed;
                this.LogGrid.Visibility = Visibility.Visible;
                this.ChatGrid.Visibility = Visibility.Visible;
                Grid.SetColumn(this.LogGrid, 0);
                Grid.SetRow(this.LogGrid, 2);
                Grid.SetColumn(this.ChatGrid, 1);
                Grid.SetRow(this.ChatGrid, 2);
                var chatGridMargin = this.ChatGrid.Margin;
                chatGridMargin.Top = 0;
                this.ChatGrid.Margin = chatGridMargin;
                var margin = this.LogGrid.Margin;
                margin.Top = 0;
                this.LogGrid.Margin = margin;
                Grid.SetColumnSpan(this.ApplicationStateGrid, 2);
                Grid.SetColumn(this.HelpButtonGrid, 1);
                Grid.SetRowSpan(this.ApplicationStateGrid, 1);
                Grid.SetRow(this.VoiceSettingsStackPanel, 0);
                Grid.SetColumn(this.VoiceSettingsStackPanel, 0);
                Grid.SetRow(this.MicrophoneSettingsStackPanel, 0);
                Grid.SetColumn(this.MicrophoneSettingsStackPanel, 1);
                Grid.SetRow(this.ConversationStateStackPanel, 0);
                Grid.SetColumn(this.ConversationStateStackPanel, 2);
                var chatAndLogGrid = ((int)this.ChatGrid.ActualWidth) + ((int)this.LogGrid.ActualWidth);
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size { Width = chatAndLogGrid, Height = 800 });
                ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = chatAndLogGrid, Height = 800 });
            }

            if (!this.WindowsContolFlyoutItem.IsChecked && !this.WindowsLogFlyoutItem.IsChecked && this.WindowsChatFlyoutItem.IsChecked)
            {
                this.ControlsGrid.Visibility = Visibility.Collapsed;
                this.LogGrid.Visibility = Visibility.Collapsed;
                this.ChatGrid.Visibility = Visibility.Visible;
                Grid.SetColumn(this.ChatGrid, 0);
                Grid.SetRow(this.ChatGrid, 2);
                var margin = this.ChatGrid.Margin;
                margin.Top = 90;
                this.ChatGrid.Margin = margin;
                Grid.SetColumnSpan(this.ChatGrid, 1);
                Grid.SetColumn(this.ApplicationStateGrid, 0);
                Grid.SetColumn(this.HelpButtonGrid, 0);
                Grid.SetRowSpan(this.ApplicationStateGrid, 3);
                Grid.SetRow(this.VoiceSettingsStackPanel, 0);
                Grid.SetColumn(this.VoiceSettingsStackPanel, 0);
                Grid.SetRow(this.MicrophoneSettingsStackPanel, 1);
                Grid.SetColumn(this.MicrophoneSettingsStackPanel, 0);
                Grid.SetRow(this.ConversationStateStackPanel, 2);
                Grid.SetColumn(this.ConversationStateStackPanel, 0);
                this.ChatGrid.HorizontalAlignment = HorizontalAlignment.Center;
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size { Width = (int)this.ChatGrid.ActualWidth - 10, Height = 800 });
                ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = (int)this.ChatGrid.ActualWidth, Height = 800 });
            }

            if (this.WindowsContolFlyoutItem.IsChecked && this.WindowsLogFlyoutItem.IsChecked && this.WindowsChatFlyoutItem.IsChecked)
            {
                this.ControlsGrid.Visibility = Visibility.Visible;
                this.LogGrid.Visibility = Visibility.Visible;
                this.ChatGrid.Visibility = Visibility.Visible;
                var controlsGridMargin = this.ControlsGrid.Margin;
                controlsGridMargin.Top = 0;
                this.ControlsGrid.Margin = controlsGridMargin;
                var chatGridMargin = this.ChatGrid.Margin;
                chatGridMargin.Top = 0;
                this.ChatGrid.Margin = chatGridMargin;
                Grid.SetColumn(this.ControlsGrid, 0);
                Grid.SetRow(this.ControlsGrid, 2);
                Grid.SetColumn(this.LogGrid, 1);
                Grid.SetRow(this.LogGrid, 2);
                Grid.SetColumn(this.ChatGrid, 2);
                Grid.SetRow(this.ChatGrid, 0);
                Grid.SetRow(this.ApplicationStateGrid, 1);
                Grid.SetColumnSpan(this.ApplicationStateGrid, 2);
                Grid.SetColumn(this.HelpButtonGrid, 1);
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size { Width = ((int)this.ControlsGrid.ActualWidth) + ((int)this.ChatGrid.ActualWidth) + ((int)this.LogGrid.ActualWidth), Height = 800 });
                ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = ((int)this.ControlsGrid.ActualWidth) + ((int)this.ChatGrid.ActualWidth) + ((int)this.LogGrid.ActualWidth), Height = 800 });
            }

            if (!this.WindowsContolFlyoutItem.IsChecked && !this.WindowsLogFlyoutItem.IsChecked && !this.WindowsChatFlyoutItem.IsChecked)
            {
                this.ControlsGrid.Visibility = Visibility.Collapsed;
                this.LogGrid.Visibility = Visibility.Collapsed;
                this.ChatGrid.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(this.ApplicationStateGrid, 2);
                Grid.SetColumn(this.HelpButtonGrid, 1);
                ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = (int)this.ApplicationStateGrid.ActualWidth, Height = (int)this.ApplicationStateGrid.ActualHeight + 100 });
            }

            if (this.WindowsContolFlyoutItem.IsChecked && !this.WindowsLogFlyoutItem.IsChecked && !this.WindowsChatFlyoutItem.IsChecked)
            {
                this.LogGrid.Visibility = Visibility.Collapsed;
                this.ChatGrid.Visibility = Visibility.Collapsed;
                this.ControlsGrid.Visibility = Visibility.Visible;
                var margin = this.ControlsGrid.Margin;
                margin.Top = 90;
                this.ControlsGrid.Margin = margin;
                Grid.SetColumnSpan(this.ControlsGrid, 1);
                Grid.SetColumn(this.ApplicationStateGrid, 0);
                Grid.SetColumn(this.HelpButtonGrid, 0);
                Grid.SetRowSpan(this.ApplicationStateGrid, 3);
                Grid.SetRow(this.VoiceSettingsStackPanel, 0);
                Grid.SetColumn(this.VoiceSettingsStackPanel, 0);
                Grid.SetRow(this.MicrophoneSettingsStackPanel, 1);
                Grid.SetColumn(this.MicrophoneSettingsStackPanel, 0);
                Grid.SetRow(this.ConversationStateStackPanel, 2);
                Grid.SetColumn(this.ConversationStateStackPanel, 0);
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size { Width = (int)this.ControlsGrid.ActualWidth, Height = 800 });
                ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = (int)this.ControlsGrid.ActualWidth, Height = 800 });
            }

            if (!this.WindowsContolFlyoutItem.IsChecked && this.WindowsLogFlyoutItem.IsChecked && !this.WindowsChatFlyoutItem.IsChecked)
            {
                this.ControlsGrid.Visibility = Visibility.Collapsed;
                this.LogGrid.Visibility = Visibility.Visible;
                this.ChatGrid.Visibility = Visibility.Collapsed;
                var margin = this.LogGrid.Margin;
                margin.Top = 90;
                this.LogGrid.Margin = margin;
                Grid.SetColumn(this.LogGrid, 0);
                Grid.SetColumn(this.ApplicationStateGrid, 0);
                Grid.SetColumn(this.HelpButtonGrid, 0);
                Grid.SetRowSpan(this.ApplicationStateGrid, 3);
                Grid.SetRow(this.VoiceSettingsStackPanel, 0);
                Grid.SetColumn(this.VoiceSettingsStackPanel, 0);
                Grid.SetRow(this.MicrophoneSettingsStackPanel, 1);
                Grid.SetColumn(this.MicrophoneSettingsStackPanel, 0);
                Grid.SetRow(this.ConversationStateStackPanel, 2);
                Grid.SetColumn(this.ConversationStateStackPanel, 0);
                ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = (int)this.LogGrid.ActualWidth, Height = 800 });
            }
        }

        private async void HelpFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            string githubReadme = @"https://github.com/Azure-Samples/Cognitive-Services-Voice-Assistant/blob/master/clients/csharp-uwp/README.md";

            var uri = new Uri(githubReadme);

            await Launcher.LaunchUriAsync(uri);
        }

        private async void DocumentationFlyoutItemClick(object sender, RoutedEventArgs e)
        {
            string mvaDocumentation = @"https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.conversationalagent?view=winrt-18362";

            var uri = new Uri(mvaDocumentation);

            await Launcher.LaunchUriAsync(uri);
        }

        private void OutputFormatComboBox_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            _ = s;
            _ = e;

            var selectedLabel = this.OutputFormatComboBox.SelectedItem.ToString();
            var selectedFormat = DialogAudio.GetMatchFromLabel(selectedLabel);
            LocalSettingsHelper.OutputFormat = selectedFormat;
        }

        private void OnChatHistoryListViewContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
            {
                return;
            }

            Conversation message = (Conversation)args.Item;
            args.ItemContainer.HorizontalAlignment = message.Sent ? Windows.UI.Xaml.HorizontalAlignment.Right : Windows.UI.Xaml.HorizontalAlignment.Left;
        }

        private async void DownloadChatHistoryClick(object sender, RoutedEventArgs e)
        {
            var json = JsonConvert.SerializeObject(this.Conversations);
            var writeChatHistory = await ApplicationData.Current.LocalFolder.CreateFileAsync($"chatHistory_{DateTime.Now.ToString("yyyyMMdd_HHmmss", null)}.json", CreationCollisionOption.ReplaceExisting);
            await File.WriteAllTextAsync(writeChatHistory.Path, json);
        }
    }
}
