// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NLog.Fluent;
    using UWPVoiceAssistantSample.AudioInput;
    using Windows.ApplicationModel.ConversationalAgent;
    using Windows.Security.Authorization.AppCapabilityAccess;
    using Windows.Storage;
    using Windows.System;
    using Windows.System.Power;
    using Windows.UI.Core;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly Queue<(string, bool)> statusBuffer;
        private readonly int statusBufferSize = 8;
        private readonly ServiceProvider services;
        private readonly ILogProvider logger;
        private readonly IKeywordRegistration keywordRegistration;
        private readonly IDialogManager dialogManager;
        private readonly IAgentSessionManager agentSessionManager;
        private App app;
        private int bufferIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.logger = LogRouter.GetClassLogger();
            this.logger.Log(LogMessageLevel.Noise, "Main page created, UI rendering");

            this.InitializeComponent();

            this.app = App.Current as App;
            this.app.HasReachedForeground = true;

            this.services = this.app.Services;
            this.dialogManager = this.services.GetRequiredService<IDialogManager>();
            this.keywordRegistration = this.services.GetRequiredService<IKeywordRegistration>();
            this.agentSessionManager = this.services.GetRequiredService<IAgentSessionManager>();

            // The "status buffer" merely exists so that we can have some structure in the
            // output log we display.
            this.statusBuffer = new Queue<(string, bool)>(this.statusBufferSize);

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

            // Wire a few pieces of UI handling that aren't trivially handled by XAML bindings
            this.AddUIHandlersAsync();

            // Ensure consistency between a few dependent controls and their settings
            this.UpdateUIBasedOnToggles();
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
            this.StartListeningButton.Click += async (_, __) =>
            {
                this.dialogManager.HandleSignalDetection(DetectionOrigin.FromPushToTalk);
                await this.UpdateUIForSharedStateAsync();
            };
            this.StopListeningButton.Click += async (_, __) =>
            {
                await this.dialogManager.FinishConversationAsync();
            };
            this.StopPlaybackButton.Click += async (_, __) =>
            {
                await this.dialogManager.StopAudioPlaybackAsync();
            };
            this.ClearButton.Click += (_, __) =>
            {
                this.statusBuffer.Clear();
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
                this.AddMessageToStatus($"\"{e}\"");
            };
            this.dialogManager.SpeechRecognized += (s, e) =>
            {
                this.AddMessageToStatus($"User: \"{e}\"");
            };
            this.dialogManager.DialogResponseReceived += (s, e) =>
            {
                // TODO: Duplicate wrapper creation unnecessary
                var wrapper = new ActivityWrapper(e.MessageBody.ToString());
                if (wrapper.Type == ActivityWrapper.ActivityType.Message)
                {
                    this.AddMessageToStatus($"Bot: \"{wrapper.Message}\"");
                }
            };
        }

        private void UpdateUIBasedOnToggles()
        {
            var useSpeechSdk = LocalSettingsHelper.EnableSpeechSDK;
            var visibility = useSpeechSdk ? Visibility.Visible : Visibility.Collapsed;
            var useKws = useSpeechSdk && LocalSettingsHelper.EnableSecondStageKws;
            var enableLogs = useSpeechSdk && LocalSettingsHelper.EnableSdkLogging;

            this.SpeechKeyTextBlock.Visibility = visibility;
            this.SpeechRegionTextBlock.Visibility = visibility;
            this.CustomSpeechIdTextBlock.Visibility = visibility;
            this.CustomVoiceIdsTextBlock.Visibility = visibility;
            this.CustomCommandsAppIdTextBlock.Visibility = visibility;
            this.BotIdTextBlock.Visibility = visibility;
            this.SpeechKeyTextBox.Visibility = visibility;
            this.SpeechRegionTextBox.Visibility = visibility;
            this.CustomSpeechIdTextBox.Visibility = visibility;
            this.CustomVoiceIdsTextBox.Visibility = visibility;
            this.CustomCommandsAppIdTextBox.Visibility = visibility;
            this.BotIdTextBox.Visibility = visibility;
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
                this.ReadBufferStream();
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

                this.StartListeningButton.IsEnabled = agentIdle && micReady;
                this.StartListeningButton.Content = micReady ? "Start Listening" : "No mic";
                this.SpeechKeyTextBox.IsEnabled = agentIdle;
                this.SpeechRegionTextBox.IsEnabled = agentIdle;
                this.CustomSpeechIdTextBox.IsEnabled = agentIdle;
                this.CustomVoiceIdsTextBox.IsEnabled = agentIdle;
                this.CustomCommandsAppIdTextBox.IsEnabled = agentIdle;
                this.BotIdTextBox.IsEnabled = agentIdle;
                this.StopListeningButton.IsEnabled = !agentIdle;

                var microphoneStatusInfo = await UIAudioStatus.GetMicrophoneStatusAsync();
                this.MicrophoneInfoIcon.Glyph = microphoneStatusInfo.Glyph;
                this.MicrophoneInfoIcon.Foreground = new SolidColorBrush(microphoneStatusInfo.Color);
                this.MicrophoneLinkButton.Content = microphoneStatusInfo.Status;

                var voiceActivationStatusInfo = await UIAudioStatus.GetVoiceActivationStatusAsync();
                this.VAStatusIcon.Glyph = voiceActivationStatusInfo.Glyph;
                this.VAStatusIcon.Foreground = new SolidColorBrush(voiceActivationStatusInfo.Color);
                this.VoiceActivationLinkButton.Content = voiceActivationStatusInfo.Status;

                this.DismissButton.Visibility = session.IsUserAuthenticated ? Visibility.Collapsed : Visibility.Visible;
            });
        }

        private async void RefreshStatus()
        {
            var itemsInQueue = this.statusBuffer.Count;
            var statusStack = new Stack<(string, bool)>(itemsInQueue + 1);
            var session = await this.agentSessionManager.GetSessionAsync().ConfigureAwait(false);
            var agentStatusMessage = session == null ?
               "No current agent session"
               : $"Current session state: {session.AgentState.ToString()} {(this.app.InvokedViaSignal ? "[via signal]" : string.Empty)}";

            // This is throwing an error on conversation state change
            for (int i = 0; i < itemsInQueue; i++)
            {
                statusStack.Push(this.statusBuffer.Peek());
                this.statusBuffer.Enqueue(this.statusBuffer.Dequeue());
            }

            var newText = string.Empty;

            while (statusStack.Count != 0)
            {
                // TODO: do something with alignment
                var (text, alignToRight) = statusStack.Pop();

                newText += !string.IsNullOrEmpty(newText) ? "\r\n" : string.Empty;
                newText += text;
            }

            _ = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.StatusText.Text = newText;
                this.ConversationState.Text = agentStatusMessage;
            });
        }

        private void AddMessageToStatus(string message, bool alignToRight = false)
        {
            if (this.statusBuffer.Count == this.statusBufferSize)
            {
                this.statusBuffer.Dequeue();
            }

            this.statusBuffer.Enqueue((message, alignToRight));

            this.RefreshStatus();
        }

        private void ReadBufferStream()
        {
            for (var i = 0; i < this.logger.LogBuffer.Count; i++)
            {
                if (this.bufferIndex < this.logger.LogBuffer.Count)
                {
                    string text = this.logger.LogBuffer[this.bufferIndex];
                    if (text.Contains(" : "))
                    {
                        string[] split = text.Split(" : ");
                        this.ChangeLogText.Text += split[1] + "\r\n";
                    }
                    else
                    {
                        this.ChangeLogText.Text += text + "\r\n";
                    }

                    this.bufferIndex++;
                }
            }
        }
    }
}
