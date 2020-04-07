// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DialogManagerTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using UWPVoiceAssistantSample;
    using UWPVoiceAssistantSampleTests;
    using Windows.ApplicationModel.ConversationalAgent;

    [TestClass]
    public class MockDialogEventsTests
    {
        private MockDialogBackend mockBackend;
        private MockKeywordRegistration mockKeywordRegistration;
        private IAgentSessionManager mockAgentSessionManager;
        private DialogManager<List<byte>> dialogManager;

        private List<string> speechRecognizedEvents;
        private List<string> speechRecognizingEvents;
        private List<string> keywordRecognizingEvents;
        private List<string> keywordRecognizedEvents;
        private List<string> signalRejectedEvents;
        private List<string> signalConfirmedEvents;
        private List<DialogResponse> dialogResponseReceivedEvents;

        private AutoResetEvent speechRecognizingEventReceived;
        private AutoResetEvent speechRecognizedEventReceived;
        private AutoResetEvent dialogResponseReceivedEventReceived;
        private AutoResetEvent keywordRecognizingEventReceived;
        private AutoResetEvent keywordRecognizedEventReceived;
        private AutoResetEvent signalRejectedEventReceived;
        private AutoResetEvent signalConfirmedEventReceived;

        [ClassInitialize]
        public static void TestClassSetup(TestContext context)
        {
        }

        [TestInitialize]
        public async Task TestMethodSetup()
        {
            MVARegistrationHelpers.UnlockLimitedAccessFeature();

            this.speechRecognizingEvents = new List<string>();
            this.speechRecognizedEvents = new List<string>();
            this.signalRejectedEvents = new List<string>();
            this.signalConfirmedEvents = new List<string>();
            this.keywordRecognizingEvents = new List<string>();
            this.keywordRecognizedEvents = new List<string>();
            this.dialogResponseReceivedEvents = new List<DialogResponse>();
            this.speechRecognizingEventReceived = new AutoResetEvent(false);
            this.speechRecognizedEventReceived = new AutoResetEvent(false);
            this.dialogResponseReceivedEventReceived = new AutoResetEvent(false);
            this.keywordRecognizingEventReceived = new AutoResetEvent(false);
            this.keywordRecognizedEventReceived = new AutoResetEvent(false);
            this.signalRejectedEventReceived = new AutoResetEvent(false);
            this.signalConfirmedEventReceived = new AutoResetEvent(false);

            this.mockBackend = new MockDialogBackend();
            this.mockKeywordRegistration = new MockKeywordRegistration();
            this.mockAgentSessionManager = new MockAgentSessionManager();
            this.dialogManager = await DialogManagerShim.CreateMockManagerAsync(this.mockBackend, this.mockKeywordRegistration, this.mockAgentSessionManager);

            this.dialogManager.SpeechRecognizing += (s, e) =>
            {
                this.speechRecognizingEvents.Add(e);
                this.speechRecognizingEventReceived.Set();
            };
            this.dialogManager.SpeechRecognized += (s, e) =>
            {
                this.speechRecognizedEvents.Add(e);
                this.speechRecognizedEventReceived.Set();
            };
            this.dialogManager.DialogStateChanged += (before, after) =>
            {
                if (before == ConversationalAgentState.Inactive && after == ConversationalAgentState.Listening)
                {
                    // Shouldn't have received anything yet; just started
                    Assert.IsTrue(this.speechRecognizingEvents.Count == 0);
                    Assert.IsTrue(this.speechRecognizedEvents.Count == 0);
                    Assert.IsTrue(this.dialogResponseReceivedEvents.Count == 0);
                }
                else if (before == ConversationalAgentState.Listening && after == ConversationalAgentState.Working)
                {
                    // Transition should happen to working (and block!) BEFORE we get the recognized event
                    Assert.IsTrue(this.speechRecognizingEvents.Count != 0);
                    Assert.IsTrue(this.speechRecognizedEvents.Count == 0);
                    Assert.IsTrue(this.dialogResponseReceivedEvents.Count == 0);
                }
                else if (before == ConversationalAgentState.Working && after == ConversationalAgentState.Inactive)
                {
                    Assert.IsTrue(this.dialogResponseReceivedEvents.Count != 0);
                }
                else if (before == ConversationalAgentState.Inactive && after == ConversationalAgentState.Inactive)
                {
                    Debug.WriteLine($"Weird transition: Inactive to Inactive");
                }
                else if (before == ConversationalAgentState.Inactive && after == ConversationalAgentState.Detecting)
                {
                    Debug.WriteLine($"Transition from inactive to detecting");
                }
                else if (before == ConversationalAgentState.Detecting && after == ConversationalAgentState.Inactive)
                {
                    Debug.WriteLine($"Transition from detecting to inactive");
                }
                else if (before == ConversationalAgentState.Listening && after == ConversationalAgentState.Detecting)
                {
                    Debug.WriteLine($"Transition from listening To detecting");
                    Assert.IsTrue(this.signalConfirmedEvents.Count == 0);
                }
                else if (before == ConversationalAgentState.Listening && after == ConversationalAgentState.Listening)
                {
                    Debug.WriteLine($"Transition from listenting to listening");
                    Assert.IsTrue(this.signalConfirmedEvents.Count != 0);
                    Assert.IsTrue(this.keywordRecognizedEvents.Count != 0);
                }
                else if (before == ConversationalAgentState.Detecting && after == ConversationalAgentState.Listening)
                {
                    Debug.WriteLine($"Transition from detecting to listening");
                }
                else if (before == ConversationalAgentState.Listening && after == ConversationalAgentState.Inactive)
                {
                    Debug.WriteLine($"Transition from listening to inactive");
                }
                else
                {
                    Assert.Fail($"Unexpected state transition: {before.ToString()} to {after.ToString()}");
                }
            };
            this.dialogManager.DialogResponseReceived += (s, e) =>
            {

                this.dialogResponseReceivedEvents.Add(e);
                this.dialogResponseReceivedEventReceived.Set();
            };

            this.dialogManager.SignalRejected += (e) =>
            {
                this.signalRejectedEvents.Add(e.ToString());
                this.signalRejectedEventReceived.Set();
            };

            this.dialogManager.SignalConfirmed += (e) =>
            {
                this.signalConfirmedEvents.Add(e.ToString());
                this.keywordRecognizingEvents.Add(e.ToString());
                this.keywordRecognizedEvents.Add(e.ToString());
                this.signalConfirmedEventReceived.Set();
                this.keywordRecognizingEventReceived.Set();
                this.keywordRecognizedEventReceived.Set();
            };
        }

        [TestCleanup]
        public async Task TestCleanupAsync()
        {
            var session = await this.mockAgentSessionManager.GetSessionAsync();
            await session.RequestAgentStateChangeAsync(ConversationalAgentState.Inactive);
        }

        [TestMethod]
        public void ReceivedIntermediateRecognitionsAreEvented()
        {
            var testText = "I'm recognizing some speech!";
            this.mockBackend.SimulateSpeechRecognizing(testText);

            Assert.IsTrue(this.speechRecognizingEventReceived.WaitOne(1000));
            Assert.IsTrue(this.speechRecognizingEvents.Count == 1);
            Assert.AreEqual(this.speechRecognizingEvents[0], testText);
        }

        [TestMethod]
        public async Task FinalRecognitionEntersWorkingState()
        {
            await this.dialogManager.StartConversationAsync(DetectionOrigin.FromPushToTalk, false);

            this.mockBackend.SimulateSpeechRecognizing("Intermediate text");

            var testText = "I recognized some speech!";
            this.mockBackend.SimulateSpeechRecognized(testText);

            Assert.IsTrue(this.speechRecognizedEventReceived.WaitOne(1000));
            Assert.IsTrue(this.speechRecognizedEvents.Count == 1);
            Assert.AreEqual(this.speechRecognizedEvents[0], testText);

            var session = await this.mockAgentSessionManager.GetSessionAsync();
            Assert.AreEqual(ConversationalAgentState.Working, session.AgentState);
        }

        [TestMethod]
        public async Task ActivityCanEndTurn()
        {
            await this.dialogManager.StartConversationAsync(DetectionOrigin.FromPushToTalk, false);
            this.mockBackend.SimulateSpeechRecognizing("Some intermediate text");
            this.mockBackend.SimulateSpeechRecognized("Some final text");
            var mockResponse = new DialogResponse(null, null, true, false);
            this.mockBackend.SimulateMessageResponse(mockResponse);

            Assert.IsTrue(this.speechRecognizingEventReceived.WaitOne(1000));
            Assert.IsTrue(this.speechRecognizedEventReceived.WaitOne(1000));
            Assert.IsTrue(this.dialogResponseReceivedEventReceived.WaitOne(1000));

            Assert.IsTrue(this.speechRecognizingEvents.Count == 1);
            Assert.IsTrue(this.speechRecognizedEvents.Count == 1);
            Assert.IsTrue(this.dialogResponseReceivedEvents.Count == 1);
            var session = await this.mockAgentSessionManager.GetSessionAsync();
            Assert.IsTrue(session.AgentState == ConversationalAgentState.Inactive);
        }

        [TestMethod]
        public async Task CanSignalRejectedEventOccur()
        {
            await this.dialogManager.InitializeAsync();

            var session = await this.mockAgentSessionManager.GetSessionAsync();
            session.IsSignalVerificationRequired = true;

            this.dialogManager.HandleSignalDetection();

            session = await this.mockAgentSessionManager.GetSessionAsync();

            Assert.IsTrue(this.signalRejectedEventReceived
                .WaitOne((int)SignalDetectionHelper.SignalConfirmationTimeout.TotalMilliseconds + 200));
            Assert.AreEqual(ConversationalAgentState.Inactive, session.AgentState);
        }

        [TestMethod]
        public async Task CanSignalConfirmedEventOccurFromPushToTalk()
        {
            await this.dialogManager.InitializeAsync();

            this.dialogManager.HandleSignalDetection(DetectionOrigin.FromPushToTalk);
            var session = await this.mockAgentSessionManager.GetSessionAsync();

            Assert.IsTrue(this.signalConfirmedEventReceived.WaitOne(1000));
            Assert.IsTrue(this.signalConfirmedEvents.Count == 1);
            Assert.IsTrue(this.keywordRecognizedEvents.Count == 1);
            Assert.AreEqual(ConversationalAgentState.Listening, session.AgentState);
        }

        [TestMethod]
        public async Task CanSignalConfirmedEventOccurFromBackgroundTask()
        {
            var session = await this.mockAgentSessionManager.GetSessionAsync();

            if (session.AgentState == ConversationalAgentState.Listening)
            {
                await session.RequestAgentStateChangeAsync(ConversationalAgentState.Inactive);
            }
            await this.dialogManager.InitializeAsync();

            await session.RequestAgentStateChangeAsync(ConversationalAgentState.Detecting);

            this.dialogManager.HandleSignalDetection(DetectionOrigin.FromBackgroundTask);

            this.mockBackend.SimulateKeywordRecognized("Contoso");
            session = await this.mockAgentSessionManager.GetSessionAsync();

            Assert.IsTrue(this.signalConfirmedEventReceived.WaitOne((int)SignalDetectionHelper.SignalConfirmationTimeout.TotalMilliseconds + 200));
            Assert.IsTrue(this.keywordRecognizedEvents.Count == 1);
            Assert.IsTrue(this.signalConfirmedEvents.Count == 1);
            Assert.AreEqual(ConversationalAgentState.Listening, session.AgentState);
        }

        [TestMethod]
        public async Task FinalSignalConfirmedEventListeningState()
        {
            await this.dialogManager.InitializeAsync();

            var session = await this.mockAgentSessionManager.GetSessionAsync();
            session.IsSignalVerificationRequired = true;

            await session.RequestAgentStateChangeAsync(ConversationalAgentState.Detecting);

            this.mockBackend.SimulateKeywordRecognizing("Contoso");
            this.mockBackend.SimulateKeywordRecognized("Contoso");

            Assert.IsTrue(this.signalConfirmedEventReceived.WaitOne(1000));
            Assert.IsTrue(this.keywordRecognizingEvents.Count == 1);
            Assert.IsTrue(this.keywordRecognizedEvents.Count == 1);
            Assert.IsTrue(this.signalConfirmedEvents.Count == 1);
            Assert.AreEqual(ConversationalAgentState.Listening, session.AgentState);
        }

        [TestMethod]
        public async Task CanSignalRejectUponKeywordConfirmation()
        {
            await this.dialogManager.InitializeAsync();

            var session = await this.mockAgentSessionManager.GetSessionAsync();
            session.IsSignalVerificationRequired = true;

            this.dialogManager.HandleSignalDetection();

            this.mockBackend.SimulateKeywordRecognizing("Contoso");
            this.mockBackend.SimulateKeywordRecognized("Contoso");

            Assert.IsTrue(this.signalRejectedEventReceived
                .WaitOne((int)SignalDetectionHelper.SignalConfirmationTimeout.TotalMilliseconds + 200));
            Assert.AreEqual(ConversationalAgentState.Inactive, session.AgentState);
        }

        [TestMethod]
        public async Task CanSignalConfirmFromTwoDetectionOrigins()
        {
            await this.dialogManager.InitializeAsync();

            this.dialogManager.HandleSignalDetection(DetectionOrigin.FromPushToTalk);
            this.dialogManager.HandleSignalDetection(DetectionOrigin.FromPushToTalk);

            var session = await this.mockAgentSessionManager.GetSessionAsync();

            Assert.IsTrue(this.signalConfirmedEventReceived.WaitOne(1000));
            Assert.IsTrue(this.signalConfirmedEvents.Count == 1);
            Assert.AreEqual(ConversationalAgentState.Listening, session.AgentState);
        }
    }
}
