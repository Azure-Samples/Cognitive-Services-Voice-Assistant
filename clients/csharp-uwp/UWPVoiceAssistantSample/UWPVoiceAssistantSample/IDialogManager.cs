// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Base abstract class. Contains the common methods in both MVA only and MVA+DLS only scenarios.
    /// </summary>
    public interface IDialogManager : IDisposable
    {
        /// <summary>
        /// Raised when the state machine for conversational agent state has finished setting
        /// a new state after requests to or responses from the dialog backend have required it.
        /// </summary>
        event DialogStateChangeEventArgs DialogStateChanged;

        /// <summary>
        /// Event raised when it is confirmed that a signal contained a keyword.
        /// runtime.
        /// </summary>
        event SignalResolutionEventArgs SignalConfirmed;

        /// <summary>
        /// Event raised when it is confirmed that a signal did not contain a keyword.
        /// </summary>
        event SignalResolutionEventArgs SignalRejected;

        /// <summary>
        /// Raised when the backend produces an intermediate "hypothesis" for the ongoing speech-
        /// to-text it's currently performing. These results can change as the user continues
        /// speaking and the use of the text should be limited to low-error-impact utilization like
        /// a responsive visualization of what's being spoken.
        /// </summary>
        event EventHandler<string> SpeechRecognizing;

        /// <summary>
        /// Raised when the backend produces its final speech-to-text result for a user utterance.
        /// This is the same text provided to the assistant implementation for business logic
        /// processing and can be considered as necessary for further client-side actions.
        /// </summary>
        event EventHandler<string> SpeechRecognized;

        /// <summary>
        /// Raised when the backend produces an assistant-implementation-specific message for
        /// the client to consume and act on. The format of these responses may vary, but
        /// typically appear as JSON payloads with schemas defined by the backend being used.
        /// </summary>
        event EventHandler<DialogResponse> DialogResponseReceived;

        /// <summary>
        /// Processes a 1st-stage activation signal as received by the conversational agent
        /// activation runtime.
        /// </summary>
        /// <param name="detectionOrigin"> The entry point through which handler received the activation signal (e.g. via background task or in-app event handler). </param>
        void HandleSignalDetection(DetectionOrigin detectionOrigin = DetectionOrigin.FromBackgroundTask);

        /// <summary>
        /// Sends an activity as defined by a derived class.
        /// </summary>
        /// <param name="activityJson"> The activity, encoded as json. </param>
        /// <returns> A task that completes once the activity is sent. </returns>
        Task<string> SendActivityAsync(string activityJson);

        /// <summary>
        /// Method invoked at the end of conversation, when no subsequent interaction is needed.
        /// </summary>
        /// <returns> A task that completes when the conversation is fully finished. </returns>
        Task FinishConversationAsync();

        /// <summary>
        /// Stops the flow of audio from the input audio graph and, if created and available,
        /// refreshes the header information on a debug audio capture file to facilitate easy
        /// use of the file in media player applications.
        /// </summary>
        /// <returns> A task that completes once audio capture has been stopped. </returns>
        Task StopAudioCaptureAsync();

        /// <summary>
        /// Stops the playback of audio on the current dialog backend audio playback provider,
        /// if available.
        /// </summary>
        /// <returns> A task that completes once playback is stopped. </returns>
        Task StopAudioPlaybackAsync();
    }
}
