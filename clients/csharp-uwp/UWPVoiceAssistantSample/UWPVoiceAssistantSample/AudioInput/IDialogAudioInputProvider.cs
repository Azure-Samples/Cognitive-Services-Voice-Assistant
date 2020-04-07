// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.ConversationalAgent;

    /// <summary>
    /// Defines the contract for a provider of audio input to a conversational agent.
    /// </summary>
    /// <typeparam name="TInputType">Input type of audio.</typeparam>
    public interface IDialogAudioInputProvider<TInputType>
        : IDisposable
    {
        /// <summary>
        /// Raised when new data is available from the underlying input.
        /// </summary>
        event Action<TInputType> DataAvailable;

        /// <summary>
        /// Gets or sets a value indicating whether debug audio files containing all outgoing
        /// audio should be created.
        /// </summary>
        bool DebugAudioCaptureFilesEnabled { get; set; }

        /// <summary>
        /// Starts an audio producer from now rather than from an agent's cached position its
        /// last session. Useful for push-to-talk and other on-demand scenarios.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task InitializeFromNowAsync();

        /// <summary>
        /// Creates an Audio Graph with defaultEncoding property to generate a generic wave file header.
        /// </summary>
        /// <param name="session">Conversation session state.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <typeparam name="TInputType">Input type of audio.</typeparam>
        Task InitializeFromAgentSessionAsync(IAgentSessionWrapper session);

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
