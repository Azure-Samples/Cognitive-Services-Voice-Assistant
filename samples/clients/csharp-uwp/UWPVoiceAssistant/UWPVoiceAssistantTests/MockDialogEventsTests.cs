
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UWPVoiceAssistant;
using UWPVoiceAssistantTests;
using Windows.ApplicationModel.ConversationalAgent;

namespace DialogManagerTests
{
    [TestClass]
    public class MockDialogEventsTests
    {
        private MockDialogBackend mockBackend;
        private DialogManager dialogManager;

        private List<string> speechRecognizedEvents;
        private List<string> speechRecognizingEvents;
        private List<DialogResponse> dialogResponseReceivedEvents;

        private AutoResetEvent speechRecognizingEventReceived;
        private AutoResetEvent speechRecognizedEventReceived;
        private AutoResetEvent dialogResponseReceivedEventReceived;

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
            this.dialogResponseReceivedEvents = new List<DialogResponse>();
            this.speechRecognizingEventReceived = new AutoResetEvent(false);
            this.speechRecognizedEventReceived = new AutoResetEvent(false);
            this.dialogResponseReceivedEventReceived = new AutoResetEvent(false);

            this.mockBackend = new MockDialogBackend();
            this.dialogManager = await DialogManagerShim.CreateMockManagerAsync(this.mockBackend);

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
        }

        [TestCleanup]
        public async Task TestCleanupAsync()
        {
            var session = await AppSharedState.GetSessionAsync();
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

            var session = await ConversationalAgentSession.GetCurrentSessionAsync();
            Assert.AreEqual(session.AgentState, ConversationalAgentState.Working);
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
            var session = await ConversationalAgentSession.GetCurrentSessionAsync();
            Assert.IsTrue(session.AgentState == ConversationalAgentState.Inactive);
        }
    }
}
