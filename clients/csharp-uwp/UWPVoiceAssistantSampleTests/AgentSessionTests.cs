// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AgentSessionTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using UWPVoiceAssistantSampleTests;
    using Windows.ApplicationModel.ConversationalAgent;

    [TestClass]
    public class AgentSessionTests
    {
        private ConversationalAgentSession agentSession;

        [TestInitialize]
        public async Task TestMethodSetup()
        {
            agentSession = await ConversationalAgentSession.GetCurrentSessionAsync();
        }

        [TestCleanup]
        public async Task TestCleanupAsync()
        {
            await agentSession.RequestAgentStateChangeAsync(ConversationalAgentState.Inactive);
            agentSession.Dispose();
        }

        [TestMethod]
        public async Task TestAgentSession()
        {
            SessionStartedProperly();

            await CanTransitionBetweenStates();
            await CanToggleInterruptibility();
            await CanGetAndSetValidSignalModelIds();
            await CannotSetUnsupportedSignalModelId();
            await ForegroundActivationSucceedsWhenActive();
            await ForegroundActivationDoesNotSucceedWhenInactive();
            await DisposingResetsToInactiveStateFromAnyState();
        }

        private async Task CanTransitionBetweenStates()
        {
            await ExecuteStateTransitions(new List<ConversationalAgentState>(new ConversationalAgentState[]
            {
                ConversationalAgentState.Speaking,
                //ConversationalAgentState.ListeningAndSpeaking,
                ConversationalAgentState.Speaking,

                ConversationalAgentState.Working,
                ConversationalAgentState.Speaking,
                ConversationalAgentState.Working,
                //ConversationalAgentState.ListeningAndSpeaking,
                ConversationalAgentState.Working,

                ConversationalAgentState.Listening,
                ConversationalAgentState.Working,
                ConversationalAgentState.Listening,
                ConversationalAgentState.Speaking,
                ConversationalAgentState.Listening,
                //ConversationalAgentState.ListeningAndSpeaking,
                ConversationalAgentState.Listening,

                ConversationalAgentState.Detecting,
                ConversationalAgentState.Listening,
                ConversationalAgentState.Detecting,
                ConversationalAgentState.Working,
                ConversationalAgentState.Detecting,
                ConversationalAgentState.Speaking,
                ConversationalAgentState.Detecting,
                //ConversationalAgentState.ListeningAndSpeaking,
                ConversationalAgentState.Detecting,

                ConversationalAgentState.Inactive,
                ConversationalAgentState.Detecting,
                ConversationalAgentState.Inactive,
                ConversationalAgentState.Listening,
                ConversationalAgentState.Inactive,
                ConversationalAgentState.Working,
                ConversationalAgentState.Inactive,
                ConversationalAgentState.Speaking,
                ConversationalAgentState.Inactive,
                //ConversationalAgentState.ListeningAndSpeaking,
                ConversationalAgentState.Inactive
            }));
        }

        private async Task CanToggleInterruptibility()
        {
            await SetInterruptible(true);
            await SetInterruptible(false);
            await SetInterruptible(true);
        }

        private async Task CanGetAndSetValidSignalModelIds()
        {
            var supportedSignalModels = await agentSession.GetSupportedSignalModelIdsAsync();

            foreach (uint signalModel in supportedSignalModels)
            {
                var successResponse = await agentSession.SetSignalModelIdAsync(signalModel);
                Assert.IsTrue(successResponse);
            }

            uint exampleModelId = 1033;
            var response = await agentSession.SetSignalModelIdAsync(exampleModelId);
            var signalModelId = await agentSession.GetSignalModelIdAsync();

            Assert.AreEqual(exampleModelId, signalModelId);
        }

        private async Task CannotSetUnsupportedSignalModelId()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            {
                await agentSession.SetSignalModelIdAsync(9999);
            });
        }

        /// <summary>
        /// Ensures that calling Dispose resets the session to its initial state regardless of the current state
        /// </summary>
        private async Task DisposingResetsToInactiveStateFromAnyState()
        {
            foreach (ConversationalAgentState state in
                new List<ConversationalAgentState>(new ConversationalAgentState[]
                {
                    ConversationalAgentState.Detecting,
                    ConversationalAgentState.Inactive,
                    ConversationalAgentState.Listening,
                    //ConversationalAgentState.ListeningAndSpeaking,
                    ConversationalAgentState.Speaking,
                    ConversationalAgentState.Working
                }))
            {
                await ValidStateTransition(state);
                agentSession.Dispose();
                // todo: trigger activation and verify that only the background signal is fired 
                agentSession = await ConversationalAgentSession.GetCurrentSessionAsync();
                SessionStartedProperly();
            }
        }

        private async Task ForegroundActivationSucceedsWhenActive()
        {
            await ValidStateTransition(ConversationalAgentState.Listening);
            var activated = new AutoResetEvent(false);
            EventBus.ReceivedForegroundActivationEvent += () => activated.Set();
            var response = await agentSession.RequestForegroundActivationAsync();

            Assert.IsTrue(activated.WaitOne(5000));
        }

        private async Task ForegroundActivationDoesNotSucceedWhenInactive()
        {
            var activated = new AutoResetEvent(false);
            EventBus.ReceivedForegroundActivationEvent += () => activated.Set();
            var response = await agentSession.RequestForegroundActivationAsync();

            Assert.IsTrue(activated.WaitOne(5000));
        }

        private void SessionStartedProperly()
        {
            Assert.AreEqual(ConversationalAgentState.Inactive, agentSession.AgentState);
            Assert.IsTrue(agentSession.IsUserAuthenticated);
            Assert.IsFalse(agentSession.IsInterrupted);
        }

        private async Task ExecuteStateTransitions(List<ConversationalAgentState> stateList)
        {
            foreach (ConversationalAgentState state in stateList)
            {
                await ValidStateTransition(state);
            }
        }

        private async Task ValidStateTransition(ConversationalAgentState newState)
        {
            var response = await agentSession.RequestAgentStateChangeAsync(newState);
            Assert.AreEqual(newState, agentSession.AgentState);
            Assert.AreEqual(ConversationalAgentSessionUpdateResponse.Success, response);
        }

        private async Task SetInterruptible(bool isInterruptible)
        {
            var response = await agentSession.RequestInterruptibleAsync(isInterruptible);
            Assert.AreEqual(isInterruptible, agentSession.IsInterruptible);
            Assert.AreEqual(ConversationalAgentSessionUpdateResponse.Success, response);
        }
    }
}
