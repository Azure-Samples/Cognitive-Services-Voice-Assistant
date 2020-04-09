// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSampleTests
{
    using UWPVoiceAssistantSample;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class DialogManagerShim
        : DialogManager<List<byte>>
    {
        protected DialogManagerShim(
            IDialogBackend<List<byte>> backend,
            IKeywordRegistration keywordRegistration,
            DialogAudioOutputAdapter outputAdapter) :
        base(
            backend,
            keywordRegistration,
            new AgentAudioInputProvider(),
            new AgentSessionManager(),
            dialogAudioOutput: outputAdapter)
        {

        }

        public static async Task<DialogManager<List<byte>>> CreateMockManagerAsync(
            IDialogBackend<List<byte>> backend,
            IKeywordRegistration keywordRegistration,
            IAgentSessionManager agentSessionManager)
        {
            var dialogManager = new DialogManager<List<byte>>(backend, keywordRegistration, new AgentAudioInputProvider(), agentSessionManager);
            await dialogManager.InitializeAsync();
            return dialogManager;
        }
    }
}
