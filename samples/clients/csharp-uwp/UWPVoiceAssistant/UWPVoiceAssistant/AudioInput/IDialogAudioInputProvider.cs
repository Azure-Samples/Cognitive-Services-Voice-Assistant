// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistant
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the contract for a provider of audio input to a conversational agent.
    /// </summary>
    public interface IDialogAudioInputProvider
        : IDisposable
    {
        /// <summary>
        /// Raised when new data is available from the underlying input.
        /// </summary>
        event Action<List<byte>> DataAvailable;

        /// <summary>
        /// Gets or sets a value indicating whether debug audio files containing all outgoing
        /// audio should be created.
        /// </summary>
        bool DebugAudioCaptureFilesEnabled { get; set; }

        /// <summary>
        /// Starts audio input from the beginning of the reachable data for the provider.
        /// </summary>
        /// <returns> A task that completes once audio input has begun. </returns>
        Task StartAsync();

        /// <summary>
        /// Starts audio input while also skipping the provided timespan of initial audio.
        /// </summary>
        /// <param name="offset"> The amount of time, computed from the stream data, to skip. </param>
        /// <returns> A task that completes once audio input has begun. </returns>
        Task StartWithInitialSkipAsync(TimeSpan offset);

        /// <summary>
        /// Requests that all audio flow from the provider stops as soon as possible.
        /// </summary>
        /// <returns> A task that completes once audio flow has terminated. </returns>
        Task StopAsync();
    }
}
