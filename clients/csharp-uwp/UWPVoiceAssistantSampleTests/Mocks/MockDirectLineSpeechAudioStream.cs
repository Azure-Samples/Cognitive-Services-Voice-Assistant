// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSampleTests
{
    using UWPVoiceAssistantSample;

    public class MockDirectLineSpeechAudioStream : DirectLineSpeechAudioOutputStream
    {
        public MockDirectLineSpeechAudioStream() : base(null, null)
        {
        }
    }
}