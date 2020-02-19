// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.ApplicationModel.Background;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private BackgroundTaskDeferral deferral;
        private ILogProvider logger;

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
            var helper = SignalDetectionHelper.Instance;
            helper.SignalConfirmed += this.OnSignalConfirmed;
            helper.SignalRejected += this.OnSignalRejected;
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
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
                Debug.WriteLine($"OnBackgroundActivated: 1st-stage keyword activation");
                SignalDetectionHelper.Instance.HandleSignalDetection();

                // NOTE: this will be restored in a future OS update.
                // this.deferral.Complete();
                // this.deferral = null;
            }
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
            Debug.WriteLine($"Suspending! {timeUntilDeadline.ToString()}");

            Task.Run(async () =>
            {
                var dialogManager = await DialogManager.GetInstanceAsync();
                await dialogManager.FinishConversationAsync();
                var session = await AppSharedState.GetSessionAsync();
                session?.Dispose();
            });

            e.SuspendingOperation.GetDeferral().Complete();
        }

        private async void OnSignalConfirmed(DetectionOrigin origin)
        {
            if (!AppSharedState.HasReachedForeground)
            {
                // At this point, an agent may choose to take whatever action it'd like to for a
                // confirmed signal in a background task. Audio should already be flowing. Most
                // commonly, this is when an application can be brought to the foreground.
                var session = await AppSharedState.GetSessionAsync();
                await session.RequestForegroundActivationAsync();
            }
        }

        private void OnSignalRejected(DetectionOrigin origin)
        {
            if (!AppSharedState.HasReachedForeground)
            {
                Debug.WriteLine($"Exiting application forcibly.");

                Task.Run(async () =>
                {
                    var session = await AppSharedState.GetSessionAsync();
                    session?.Dispose();
                }).Wait();

                Application.Current.Exit();
            }
        }

        private void AddVersionToTitle()
        {
            var view = ApplicationView.GetForCurrentView();
            var ver = Package.Current.Id.Version;
            view.Title = $"Agent v{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}";
        }
    }
}
