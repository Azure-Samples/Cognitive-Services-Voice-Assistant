// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using UWPVoiceAssistantSample.AudioOutput;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.ApplicationModel.Background;
    using Windows.ApplicationModel.ConversationalAgent;
    using Windows.ApplicationModel.Core;
    using Windows.Storage;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application, IDisposable
    {
        private readonly ILogProvider logger;
        private readonly IDialogManager dialogManager;
        private readonly IAgentSessionManager agentSessionManager;
        private BackgroundTaskDeferral deferral;
        private bool alreadyDisposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            LogRouter.Initialize();
            this.logger = LogRouter.GetClassLogger();
            this.logger.Log("Constructor: app launched");

            this.Suspending += this.OnSuspending;
            MVARegistrationHelpers.UnlockLimitedAccessFeature();

            this.CopyConfigAndAssignValues().GetAwaiter();

            var keywordRegistration = new KeywordRegistration(
                "Contoso",
                "{C0F1842F-D389-44D1-8420-A32A63B35568}",
                "1033",
                "MICROSOFT_KWSGRAPH_V1",
                "ms-appx:///MVAKeywords/Contoso.bin",
                new Version(1, 0, 0, 0),
                "ms-appx:///SDKKeywords/Contoso.table");

            this.agentSessionManager = new AgentSessionManager();

            this.dialogManager = new DialogManager<List<byte>>(
                new DirectLineSpeechDialogBackend(),
                keywordRegistration,
                new AgentAudioInputProvider(),
                this.agentSessionManager,
                new MediaPlayerDialogAudioOutputAdapter());

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(this.dialogManager);
            serviceCollection.AddSingleton<IKeywordRegistration>(keywordRegistration);
            serviceCollection.AddSingleton(this.agentSessionManager);
            this.Services = serviceCollection.BuildServiceProvider();

            CoreApplication.Exiting += async (object sender, object args) =>
            {
                this.logger.Log($"Exiting!");
                await this.dialogManager.FinishConversationAsync();
                var session = await this.agentSessionManager.GetSessionAsync();
                session?.Dispose();
                this.logger.Log("Exited");
            };

            this.InitializeSignalDetection();
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets a value indicating whether value of ConversationalSessionState if application is activated using keyword.
        /// </summary>
        public bool InvokedViaSignal { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the application has reached a foreground state
        /// where control of application lifecycle should not be passively done on behalf of the
        /// user.
        /// </summary>
        public bool HasReachedForeground { get; set; } = false;

        /// <summary>
        /// Gets service provider that contains services shared across views.
        /// </summary>
        public ServiceProvider Services { get; }

        /// <summary>
        /// Disposes of underlying managed resources. Standard implementation of IDisposable.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size { Width = 1400, Height = 800 });

            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null && e != null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                rootFrame.NavigationFailed += this.OnNavigationFailed;
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e?.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), null);
                }

                // Ensure the current window is active
                Window.Current.Activate();
            }

            this.AddVersionToTitle();
        }

        /// <summary>
        /// Invoked when the application is activated from either Background or Foreground. Upon Activation,
        /// Application MainPage is updated.
        /// </summary>
        /// <param name="args">RootFrame.</param>
        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);

            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += this.OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), null);
            }

            this.AddVersionToTitle();
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Application is activated from the background to begin the ConversationalSessionState.
        /// The application must be registered using LAF and the BackgroundTriggerName must mach the args event.
        /// </summary>
        /// <param name="args">The BackgroundTriggerName registered by the application.</param>
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            this.deferral = args?.TaskInstance.GetDeferral();

            if (args?.TaskInstance.Task.Name == MVARegistrationHelpers.BackgroundTriggerName)
            {
                this.logger.Log($"OnBackgroundActivated: 1st-stage keyword activation");
                this.dialogManager.HandleSignalDetection();

                // NOTE: this will be restored in a future OS update.
                // this.deferral.Complete();
                // this.deferral = null;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!this.alreadyDisposed)
            {
                if (disposing)
                {
                    this.dialogManager?.Dispose();
                    this.Services?.Dispose();
                }

                this.alreadyDisposed = true;
            }
        }

        private void InitializeSignalDetection()
        {
            this.dialogManager.SignalConfirmed += this.OnSignalConfirmed;
            this.dialogManager.SignalRejected += this.OnSignalRejected;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails.
        /// </summary>
        /// <param name="sender">The Frame which failed navigation.</param>
        /// <param name="e">Details about the navigation failure.</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deadline = e.SuspendingOperation.Deadline;
            var timeUntilDeadline = deadline.Subtract(DateTime.Now);
            this.logger.Log($"Suspending! {timeUntilDeadline.ToString()}");

            Task.Run(async () =>
            {
                await this.dialogManager.FinishConversationAsync();
                var session = await this.agentSessionManager.GetSessionAsync();
                session?.Dispose();
                this.logger.Log("Suspended");
            });

            e.SuspendingOperation.GetDeferral().Complete();
        }

        private async void OnSignalConfirmed(DetectionOrigin origin)
        {
            if (!this.HasReachedForeground)
            {
                // At this point, an agent may choose to take whatever action it'd like to for a
                // confirmed signal in a background task. Audio should already be flowing. Most
                // commonly, this is when an application can be brought to the foreground.
                var session = await this.agentSessionManager.GetSessionAsync();
                await session.RequestForegroundActivationAsync();
            }
        }

        private void OnSignalRejected(DetectionOrigin origin)
        {
            if (!this.HasReachedForeground)
            {
                this.logger.Log($"Exiting application forcibly.");
                WindowService.CloseWindow();
            }
        }

        private void AddVersionToTitle()
        {
            var view = ApplicationView.GetForCurrentView();
            var ver = Package.Current.Id.Version;
            view.Title = $"Agent v{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";
        }

        private async Task CopyConfigAndAssignValues()
        {
            var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///config.json"));

            if (!string.IsNullOrWhiteSpace(configFile.Path))
            {
                AppSettings appSettings = AppSettings.Load(configFile.Path);

                LocalSettingsHelper.SpeechSubscriptionKey = appSettings.SpeechSubscriptionKey;
                LocalSettingsHelper.AzureRegion = appSettings.AzureRegion;
                LocalSettingsHelper.CustomSpeechId = appSettings.CustomSpeechId;
                LocalSettingsHelper.CustomVoiceIds = appSettings.CustomVoiceIds;
                LocalSettingsHelper.CustomCommandsAppId = appSettings.CustomCommandsAppId;
                LocalSettingsHelper.BotId = appSettings.BotId;
            }
        }
    }
}
