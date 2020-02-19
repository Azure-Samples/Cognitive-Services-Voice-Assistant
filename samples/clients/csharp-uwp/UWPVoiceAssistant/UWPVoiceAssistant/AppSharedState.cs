// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.ConversationalAgent;

    /// <summary>
    /// Verifies if background processess and permissions are enabled to manage the application state.
    /// </summary>
    public static class AppSharedState
    {
        private static readonly SemaphoreSlim CachedSessionSemaphore = new SemaphoreSlim(1, 1);
        private static ConversationalAgentSession cachedAgentSession = null;

        /// <summary>
        /// Gets or sets a value indicating whether value of ConversationalSessionState if application is activated using keyword.
        /// </summary>
        public static bool InvokedViaSignal { get; set; } = false;

        /// <summary>
        /// Gets the selected keyword information to use across the application.
        /// </summary>
        public static KeywordRegistration KeywordInfo { get; } = KeywordRegistration.Contoso;

        /// <summary>
        /// Gets or sets the activation keyword configuration representation for the application.
        /// This is the configuratio that represents the "first stage" keyword spotter that can
        /// launch the application when it's not already running.
        /// </summary>
        public static ActivationSignalDetectionConfiguration KeywordConfiguration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current active keyword configuration is
        /// in an application-enabled state.
        /// </summary>
        public static bool KeywordEnabledByApp
        {
            get => AppSharedState.KeywordConfiguration?.AvailabilityInfo?.IsEnabled ?? false;
            set => AppSharedState.KeywordConfiguration?.SetEnabledAsync(value).AsTask().Wait();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the application has reached a foreground state
        /// where control of application lifecycle should not be passively done on behalf of the
        /// user.
        /// </summary>
        public static bool HasReachedForeground { get; set; } = false;

        /// <summary>
        /// It's not required, but for optimization purposes it's acceptable to cache the ConversationalAgentSession
        /// for the lifetime of the app and use it across background/foreground invocations.
        /// </summary>
        /// <returns>Cached Conversation session state.</returns>
        public static async Task<ConversationalAgentSession> GetSessionAsync()
        {
            await CachedSessionSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (cachedAgentSession == null)
                {
                    cachedAgentSession = await ConversationalAgentSession.GetCurrentSessionAsync();
                    cachedAgentSession.SignalDetected += OnInAppSignalEventDetected;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to configure a ConversationalAgentSession. Please check your registration with the MVA platform.\r\n{ex.Message}");
            }
            finally
            {
                CachedSessionSemaphore.Release();
            }

            return cachedAgentSession;
        }

        private static async void OnInAppSignalEventDetected(ConversationalAgentSession sender, ConversationalAgentSignalDetectedEventArgs args)
        {
            Debug.WriteLine($"'{sender.Signal.SignalName}' signal detected in session event handler");

            // Doing a "heartbeat" ensures we don't get the background event
            var session = await AppSharedState.GetSessionAsync();
            await session.RequestAgentStateChangeAsync(session.AgentState);

            SignalDetectionHelper.Instance.HandleSignalDetection(DetectionOrigin.FromApplicationObject);
        }
    }
}
