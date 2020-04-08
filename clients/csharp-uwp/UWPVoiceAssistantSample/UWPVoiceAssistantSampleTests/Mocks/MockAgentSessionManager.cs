// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSampleTests
{
    using UWPVoiceAssistantSample;
    using System;
    using System.Threading.Tasks;

    public class MockAgentSessionManager : IAgentSessionManager
    {
        public event EventHandler<DetectionOrigin> SignalDetected;

        public IAgentSessionWrapper session;

        public Task<IAgentSessionWrapper> GetSessionAsync()
        {
            this.session = this.session ?? new MockAgentSessionWrapper();
            return Task.FromResult(this.session);
        }
    }
}
