// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSampleTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using UWPVoiceAssistantSample;
    using System.Collections.Generic;
    using System.Threading;

    [TestClass]
    public class DialogResponseQueueTests
    {
        [TestMethod]
        public void QueueMulitpleVoiceResponses()
        {
            var dialogResponseReceivedEvents = new List<DialogResponse>();
            var dialogResponseExecutedEvents = new List<DialogResponse>();
            var dialogResponseReceivedEventReceived = new AutoResetEvent(false);
            var dialogResponseExecutedEventReceived = new AutoResetEvent(false);

            Initialize(
                dialogResponseReceivedEvents,
                dialogResponseExecutedEvents,
                dialogResponseReceivedEventReceived,
                dialogResponseExecutedEventReceived,
                out var queue);

            queue.Enqueue(new DialogResponse("message1", new MockDirectLineSpeechAudioStream(), false, true));

            Assert.IsTrue(dialogResponseReceivedEventReceived.WaitOne(10), "Didn't fire executingresponse for first message");
            Assert.AreEqual(1, dialogResponseReceivedEvents.Count, "Didn't execute code in executingresponse callback for first message");
            Assert.AreEqual(0, dialogResponseExecutedEvents.Count, "Fired responseexecuted callback prematurely for first message");

            queue.Enqueue(new DialogResponse("message2", new MockDirectLineSpeechAudioStream(), false, true));

            Assert.AreEqual(1, dialogResponseReceivedEvents.Count, "Fired executingresponse callback before first callback completed");

            Assert.IsTrue(
                dialogResponseExecutedEventReceived.WaitOne(MockDialogOutputAdapter.PlayLength + 100),
                "First responseexecuted callback not fired");
            Assert.IsTrue(
                 dialogResponseReceivedEventReceived.WaitOne(10),
                 "Second executingresponse callback not fired");

            Assert.AreEqual(2, dialogResponseReceivedEvents.Count, "Didn't execute code in executingresponse callback for second message");
            Assert.AreEqual(1, dialogResponseExecutedEvents.Count, "Fired responseexecuted callback prematurely for second message");
        }

        [TestMethod]
        public void QueueVoiceAndTextResponses()
        {
            var dialogResponseReceivedEvents = new List<DialogResponse>();
            var dialogResponseExecutedEvents = new List<DialogResponse>();
            var dialogResponseReceivedEventReceived = new AutoResetEvent(false);
            var dialogResponseExecutedEventReceived = new AutoResetEvent(false);

            Initialize(
                dialogResponseReceivedEvents,
                dialogResponseExecutedEvents,
                dialogResponseReceivedEventReceived,
                dialogResponseExecutedEventReceived,
                out var queue);

            queue.Enqueue(new DialogResponse("text 1", null, false, true));

            Assert.IsTrue(dialogResponseReceivedEventReceived.WaitOne(10), "Didn't fire executingresponse for text 1");
            Assert.AreEqual(1, dialogResponseReceivedEvents.Count, "Didn't hit code in executingresponse callback for text 1");
            Assert.AreEqual(1, dialogResponseExecutedEvents.Count, "Didn't hit responseexecuted callback for text 1");
            dialogResponseExecutedEventReceived.Reset();

            queue.Enqueue(new DialogResponse("voice 2", new MockDirectLineSpeechAudioStream(), false, true));

            Assert.IsTrue(dialogResponseReceivedEventReceived.WaitOne(10), "Didn't fire executingresponse for voice 2");
            Assert.AreEqual(2, dialogResponseReceivedEvents.Count, "Didn't execute code in executingresponse callback for voice 2");
            Assert.AreEqual(1, dialogResponseExecutedEvents.Count, "Fired responseexecuted callback prematurely for voice 2");

            queue.Enqueue(new DialogResponse("text 3", null, false, true));

            Assert.AreEqual(2, dialogResponseReceivedEvents.Count, "Fired executingresponse callback for text 3 before voice 2 callback completed");

            Assert.IsTrue(
                dialogResponseExecutedEventReceived.WaitOne(MockDialogOutputAdapter.PlayLength + 100),
                "Voice 2 responseexecuted callback not fired");
            Assert.IsTrue(
                dialogResponseReceivedEventReceived.WaitOne(100),
                "Text 3 executingresponse callback not fired");

            Assert.AreEqual(3, dialogResponseReceivedEvents.Count, "Didn't execute code in executingresponse callback for text 3");
        }

        [TestMethod]
        public void EnsureQueueIsThreadsafe()
        {
            var dialogResponseReceivedEvents = new List<DialogResponse>();
            var dialogResponseExecutedEvents = new List<DialogResponse>();
            var dialogResponseReceivedEventReceived = new AutoResetEvent(false);
            var dialogResponseExecutedEventReceived = new AutoResetEvent(false);

            Initialize(
                dialogResponseReceivedEvents,
                dialogResponseExecutedEvents,
                dialogResponseReceivedEventReceived,
                dialogResponseExecutedEventReceived,
                out var queue);

            var voice1 = new DialogResponse("voice 1", new MockDirectLineSpeechAudioStream(), false, true);
            var text2 = new DialogResponse("text 2", null, false, true);
            var text4 = new DialogResponse("text 3", null, false, true);
            var voice3 = new DialogResponse("voice 4", new MockDirectLineSpeechAudioStream(), false, true);

            new Thread(() =>
            {
                // Enqueue and dequeue voice 1 immediately
                queue.Enqueue(voice1);
                // Enqueue voice 3 after text 2 enqueues
                Thread.Sleep(MockDialogOutputAdapter.PlayLength / 3);
                queue.Enqueue(voice3);
            }).Start();

            new Thread(() =>
            {
                // Enqueue text 2 on a new thread immediately after queuing voice 1
                queue.Enqueue(text2);
                //Enqueue text 4 during voice 3 playback
                Thread.Sleep(MockDialogOutputAdapter.PlayLength / 2);
                queue.Enqueue(text4);
            }).Start();

            // Voice 1 should dequeue immediately, text 2 and 3 should not
            Assert.IsTrue(dialogResponseReceivedEventReceived.WaitOne(MockDialogOutputAdapter.PlayLength / 2), "Didn't fire executingresponse for voice 1");
            Assert.AreEqual(1, dialogResponseReceivedEvents.Count, "Didn't execute executingresponse callback for voice 1");
            Assert.AreEqual(voice1, dialogResponseReceivedEvents[0]);

            // Text 2 and voice 3 should dequeue in order when voice 1 completes
            Assert.IsTrue(dialogResponseReceivedEventReceived.WaitOne(MockDialogOutputAdapter.PlayLength + 100), "Didn't dequeue text 2");
            Assert.IsTrue(dialogResponseReceivedEventReceived.WaitOne(10), "Didn't dequeue voice 3");
            Assert.AreEqual(3, dialogResponseReceivedEvents.Count, "Didn't execute text 2 or voice 3 callback");
            Assert.AreEqual(text2, dialogResponseReceivedEvents[1]);
            Assert.AreEqual(voice3, dialogResponseReceivedEvents[2]);

            // Text 4 should dequeue when voice 3 completes
            Assert.IsTrue(dialogResponseReceivedEventReceived.WaitOne(MockDialogOutputAdapter.PlayLength + 100), "Didn't dequeue text 4");
            Assert.AreEqual(4, dialogResponseReceivedEvents.Count, "Didn't execute text 4 callback");
            Assert.IsTrue(dialogResponseReceivedEvents.Contains(text4));
            Assert.AreEqual(text4, dialogResponseReceivedEvents[3]);
        }

        private void Initialize(
            List<DialogResponse> dialogResponseReceivedEvents,
            List<DialogResponse> dialogResponseExecutedEvents,
            AutoResetEvent dialogResponseReceivedEventReceived,
            AutoResetEvent dialogResponseExecutedEventReceived,
            out DialogResponseQueue queue)
        {
            IDialogAudioOutputAdapter mockDialogOutputAdapter = new MockDialogOutputAdapter();
            queue = new DialogResponseQueue(mockDialogOutputAdapter);

            queue.ExecutingResponse += (DialogResponse response) =>
            {
                dialogResponseReceivedEvents.Add(response);
                dialogResponseReceivedEventReceived.Set();
            };

            queue.ResponseExecuted += (DialogResponse response) =>
            {
                dialogResponseExecutedEvents.Add(response);
                dialogResponseExecutedEventReceived.Set();
            };
        }
    }
}
