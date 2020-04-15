// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


namespace UWPVoiceAssistantSampleTests
{
    using UWPVoiceAssistantSample;
    using System;
    using System.Threading.Tasks;
    using Windows.Media.MediaProperties;

    public class MockDialogOutputAdapter : IDialogAudioOutputAdapter
    {
        public static int PlayLength = 3000;

        public AudioEncodingProperties OutputEncoding { get; set; }
            = AudioEncodingProperties.CreatePcm(16000, 1, 16);

        public bool IsPlaying { get; set; }

        public event Action OutputEnded;

        public void Dispose() { }

        public void EnqueueDialogAudio(DialogAudioOutputStream audioData)
        {
            this.IsPlaying = true;
            Task.Run(async () =>
            {
                await Task.Delay(PlayLength);
                this.OutputEnded.Invoke();
            });
        }

        public Task PlayAudioAsync(DialogAudioOutputStream stream)
        {
            this.IsPlaying = true;
            return new Task(async () =>
            {
                await Task.Delay(PlayLength);
                this.OutputEnded.Invoke();
            });
        }

        public Task StopPlaybackAsync()
        {
            this.IsPlaying = false;
            return new Task(() => { });
        }
    }
}