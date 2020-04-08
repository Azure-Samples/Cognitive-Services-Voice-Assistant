// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Threading.Tasks;
    using Windows.Storage;

    /// <summary>
    /// Describes the contract for dialog backends responsible for servicing general conversational
    /// state updates and providing responses from an assistant implementation.
    /// </summary>
    /// <typeparam name="TInputType">Input type of audio</typeparam>
    public interface IDialogBackend<TInputType>
        : IDisposable
    {
        /// <summary>
        /// Raised when a backend has started processing input.
        /// </summary>
        event Action<string> SessionStarted;

        /// <summary>
        /// Raised when a backend has stopped processing input.
        /// </summary>
        event Action<string> SessionStopped;

        /// <summary>
        /// Raised when an intermediate stage of keyword recognition or verification has been
        /// performed by the backend. Assistant implementations may choose to take staged user-
        /// perceivable action across multiple layers of confirmation or ignore intermediate stages
        /// and act only on the final, fully-confirmed recognition.
        /// </summary>
        event Action<string> KeywordRecognizing;

        /// <summary>
        /// Raised when the final keyword recognition step has been performed by the backend. An
        /// accepted result at this stage is full confirmation that user-perceived action, like
        /// showing a window and playing an earcon, is appropriate.
        /// </summary>
        event Action<string> KeywordRecognized;

        /// <summary>
        /// Raised when the backend produces an intermediate "hypothesis" for the ongoing speech-
        /// to-text it's currently performing. These results can change as the user continues
        /// speaking and the use of the text should be limited to low-error-impact utilization like
        /// a responsive visualization of what's being spoken.
        /// </summary>
        event Action<string> SpeechRecognizing;

        /// <summary>
        /// Raised when the backend produces its final speech-to-text result for a user utterance.
        /// This is the same text provided to the assistant implementation for business logic
        /// processing and can be considered as necessary for further client-side actions.
        /// </summary>
        event Action<string> SpeechRecognized;

        /// <summary>
        /// Raised when the backend produces an assistant-implementation-specific message for
        /// the client to consume and act on. The format of these responses may vary, but
        /// typically appear as JSON payloads with schemas defined by the backend being used.
        /// </summary>
        event Action<DialogResponse> DialogResponseReceived;

        /// <summary>
        /// Raised when the backend provides information about an error encountered during the
        /// servicing of a request.
        /// </summary>
        event Action<DialogErrorInformation> ErrorReceived;

        /// <summary>
        /// Gets the backend-specific confirmation model configured for use for post-1st-stage
        /// keyword spotting.
        /// </summary>
        object ConfirmationModel { get; }

        /// <summary>
        /// Sets the object used as an audio source for this backend and clears any existing
        /// state in a previous source as needed.
        /// </summary>
        /// <param name="source"> The IDialogAudioInputProvider used as an audio source to this backend. </param>
        void SetAudioSource(IDialogAudioInputProvider<TInputType> source);

        /// <summary>
        /// Instructs a backend to perform any needed preliminary initialization before which it
        /// cannot handle requests.
        /// </summary>
        /// <param name="keywordFile"> The keyword file to be loaded as part of initialization.</param>
        /// <returns> A task that completes once the backend is in a ready state. </returns>
        Task InitializeAsync(StorageFile keywordFile);

        /// <summary>
        /// Instructs the turn to begin a turn based on the input available in the current input
        /// audio source. Validates for the presence of a keyword if provided a confirmation
        /// model resource to use.
        /// </summary>
        /// <param name="performConfirmation">
        ///     Whether to use the backend-specific model resource for validating the presence of
        ///     a keyword in the input audio stream, if available. If unavailable or false,
        ///     no verification will be attempted and the input audio will be treated as
        ///     previously-confirmed.
        /// </param>
        /// <returns> A task that completes when the turn has begun and audio is flowing. </returns>
        Task StartAudioTurnAsync(bool performConfirmation);

        /// <summary>
        /// If the current turn is confirming a signal, abort the verfication.
        /// </summary>
        /// <returns> A task that completes when the in-progress turn has been aborted. </returns>
        Task CancelSignalVerification();

        /// <summary>
        /// Sends a backend-specific message to the assistant implementation for non-speech data.
        /// This can include asynchronous information to inform and influence action taken on an
        /// ongoing audio turn. Typically takes the form of a JSON payload with schema determined
        /// by the backend selection, but implementations may vary.
        /// </summary>
        /// <param name="message"> The message to send to the backend, encoded as a string. </param>
        /// <returns>
        ///     A task that completes once the message is sent and includes the interaction ID,
        ///     if applicable, associated with receipt of the message.
        /// </returns>
        Task<string> SendDialogMessageAsync(string message);
    }
}
