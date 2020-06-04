// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UWPVoiceAssistantSample.KwsPerformance;
    using Windows.ApplicationModel.ConversationalAgent;

    /// <summary>
    /// Class to wrap a single instance of ConversationalAgentSession for an app.
    /// </summary>
    public class AgentSessionManager : IAgentSessionManager, IDisposable
    {
        private readonly SemaphoreSlim cachedSessionSemaphore = new SemaphoreSlim(1, 1);
        private readonly ILogProvider logger;
        private AgentSessionWrapper cachedAgentSession = null;
        private KwsPerformanceLogger kwsPerformanceLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentSessionManager"/> class.
        /// </summary>
        public AgentSessionManager()
        {
            this.logger = LogRouter.GetClassLogger();
            this.kwsPerformanceLogger = new KwsPerformanceLogger();
        }

        /// <summary>
        /// Raised when the state machine for conversational agent state has finished setting
        /// a new state after requests to or responses from the dialog backend have required it.
        /// </summary>
        public event EventHandler<DetectionOrigin> SignalDetected;

        /// <summary>
        /// Gets a value indicating whether this object has already processed a Dispose() call.
        /// </summary>
        protected bool AlreadyDisposed { get; private set; } = false;

        /// <summary>
        /// It's not required, but for optimization purposes it's acceptable to cache the ConversationalAgentSession
        /// for the lifetime of the app and use it across background/foreground invocations.
        /// </summary>
        /// <returns>Cached Conversation session state.</returns>
        public async Task<IAgentSessionWrapper> GetSessionAsync()
        {
            await this.cachedSessionSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (this.cachedAgentSession == null)
                {
                    this.cachedAgentSession = new AgentSessionWrapper(await ConversationalAgentSession.GetCurrentSessionAsync());
                    this.cachedAgentSession.InitializeHandlers();

                    this.cachedAgentSession.SignalDetected += this.OnInAppSignalEventDetected;
                }
            }
            catch (Exception ex)
            {
                this.logger.Log(LogMessageLevel.Error, $"Unable to configure a ConversationalAgentSession. Please check your registration with the MVA platform.\r\n{ex.Message}");
            }
            finally
            {
                this.cachedSessionSemaphore.Release();
            }

            return this.cachedAgentSession;
        }

        /// <summary>
        /// This code added to correctly implement the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Free disposable resources per the IDisposable interface.
        /// </summary>
        /// <param name="disposing"> Whether managed state is being disposed. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.AlreadyDisposed)
            {
                if (disposing)
                {
                    this.cachedAgentSession?.Dispose();
                    this.cachedAgentSession = null;
                    this.cachedSessionSemaphore?.Dispose();
                }
            }
        }

        private void OnInAppSignalEventDetected(ConversationalAgentSession sender, ConversationalAgentSignalDetectedEventArgs args)
        {
            // KwsPerformanceLogger.kwsEventFireTime = DateTime.Now.Ticks;
            // this.kwsPerformanceLogger.LogSignalReceived("1", true, DateTime.Now.Ticks, startTime, endTime);
            this.logger.Log(LogMessageLevel.SignalDetection, $"'{sender.Signal.SignalName}' signal detected in session event handler");

            this.SignalDetected?.Invoke(this, DetectionOrigin.FromApplicationObject);
        }
    }
}
