// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AgentAudioInputProviderTests
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.ConversationalAgent;
    using UWPVoiceAssistantSample;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AgentAudioInputProviderTests
    {
        [TestMethod]
        public async Task GetAudioProducerFromNowAsync()
        {
            var result = await TestAgentAudioInputProvider.InitializeFromNowAsync();

            Assert.IsNull(result.AgentSession);
            Assert.IsNotNull(result.InputGraph);
            Assert.IsFalse(result.GraphRunning);
            Assert.IsFalse(result.GraphRunning);
            Assert.IsNotNull(result.InputGraph);
            Assert.IsNotNull(result.OutputEncoding);
            Assert.AreEqual((UInt32)256000, result.OutputEncoding.Bitrate);
            Assert.AreEqual((UInt32)16000, result.OutputEncoding.SampleRate);
            Assert.AreEqual("PCM", result.OutputEncoding.Subtype);
            Assert.IsNotNull(result.OutputNode);
            Assert.IsNotNull(result.InputNode);
        }

        [TestMethod]
        public async Task GetAudioProducerFromAgentSessionAsync()
        {
            var agentSessionManager = new AgentSessionManager();
            var session = agentSessionManager.GetSessionAsync().GetAwaiter().GetResult();
            var result = await TestAgentAudioInputProvider.InitializeFromAgentSessionAsync(session);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.AgentSession.AgentState == ConversationalAgentState.Inactive);
            Assert.IsTrue(result.AgentSession.IsInterruptible);
            Assert.IsTrue(result.AgentSession.IsUserAuthenticated);
            Assert.IsTrue(result.AgentSession.IsVoiceActivationAvailable);
            Assert.IsFalse(result.Disposed);
            Assert.IsFalse(result.GraphRunning);
            Assert.IsNotNull(result.InputGraph);
            Assert.IsNotNull(result.OutputNode);
            Assert.IsNotNull(result.InputNode);
            Assert.IsTrue(result.InputNode.Duration > TimeSpan.Zero);
            Assert.IsNotNull(result.InputNode.SourceFile);
        }

        [TestMethod]
        public async Task GetStartWithInitialSkipFromSession()
        {
            TestAgentAudioInputProvider testAgent = await TestAgentAudioInputProvider.InitializeFromNowAsync();
            await testAgent.StartWithInitialSkipAsync(TimeSpan.Zero);

            Debug.WriteLine(testAgent);
            Assert.IsNull(testAgent.AgentSession);
            Assert.IsFalse(testAgent.Disposed);
            Assert.IsNotNull(testAgent.InputNode);
            Assert.IsNotNull(testAgent.OutputNode);
            Assert.IsTrue(testAgent.GraphRunning);
            Assert.IsNotNull(testAgent.InputGraph);
        }

        [TestMethod]
        public async Task GetStopAsyncFromSession()
        {
            TestAgentAudioInputProvider testAgent = await TestAgentAudioInputProvider.InitializeFromNowAsync();
            await testAgent.StartWithInitialSkipAsync(TimeSpan.Zero);

            Assert.IsNull(testAgent.AgentSession);
            Assert.IsFalse(testAgent.Disposed);
            Assert.IsNotNull(testAgent.OutputNode);
            Assert.IsNotNull(testAgent.OutputEncoding);
            Assert.IsTrue(testAgent.GraphRunning);
            Assert.IsNotNull(testAgent.InputGraph);

            await testAgent.StopAsync();

            Assert.IsFalse(testAgent.GraphRunning);
        }

        [TestMethod]
        public async Task GetBytesFromDataAvailble()
        {
            TestAgentAudioInputProvider testAgent = await TestAgentAudioInputProvider.InitializeFromNowAsync();
            testAgent.DebugAudioCaptureFilesEnabled = true;

            await testAgent.StartWithInitialSkipAsync(TimeSpan.Zero);

            Assert.IsNotNull(testAgent.DebugAudioOutputFileStream);
            Assert.IsTrue(testAgent.DebugAudioOutputFileStream.Length > 0);

            await testAgent.StopAsync();
        }

        [TestCleanup]
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
