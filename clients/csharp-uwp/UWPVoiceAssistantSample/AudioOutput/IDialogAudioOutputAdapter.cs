// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Threading.Tasks;
    using Windows.Media.MediaProperties;

    /// <summary>
    /// Responsible for Creating and maintaining the Audio Graph for comminucation with Direct Line Speech.
    /// </summary>
    public interface IDialogAudioOutputAdapter : IDisposable
    {
        /// <summary>
        /// Raised when all enqueued audio has completed playback.
        /// </summary>
        event Action OutputEnded;

        /// <summary>
        /// Gets a value indicating whether the adapter is currently playing audio.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Gets or sets the encoding to be used for output. Should match the expected format of data being provided.
        /// </summary>
        AudioEncodingProperties OutputEncoding { get; set; }

        /// <summary>
        /// Cancels any current playback on the adapter and asynchronously begins playback of the provided Speech SDK
        /// dialog output audio.
        /// </summary>
        /// <param name="stream"> The output stream to play. </param>
        /// <returns> A task that completes once the pending output is completed. </returns>
        Task PlayAudioAsync(DialogAudioOutputStream stream);

        /// <summary>
        /// Ends Audio Playback and regenerates Audio Graph with corresponding Input and Output Nodes.
        /// </summary>
        /// <returns> A task that completes once playback has stopped. </returns>
        Task StopPlaybackAsync();
    }
}