// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample
{
    /// <summary>
    /// A generic encapsulation of the data available on a dialog response.
    /// Business logic will be specific to individual backends.
    /// </summary>
    public class DialogResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogResponse"/> class.
        /// </summary>
        /// <param name="messageBody"> The body of the dialog response. </param>
        /// <param name="messageMedia"> Optional media associated with the dialog response. </param>
        /// <param name="shouldEndTurn"> Whether the response indicates the turn should end. </param>
        /// <param name="shouldStartNewTurn"> Whether the response indicates a new turn should follow. </param>
        public DialogResponse(
            object messageBody,
            DialogAudioOutputStream messageMedia,
            bool shouldEndTurn,
            bool shouldStartNewTurn)
        {
            this.MessageBody = messageBody;
            this.MessageMedia = messageMedia;
            this.TurnEndIndicated = shouldEndTurn;
            this.FollowupTurnIndicated = shouldStartNewTurn;
        }

        /// <summary>
        /// Gets the body of the dialog response, typically a JSON document that follows a
        /// backend-specific schema.
        /// </summary>
        public object MessageBody { get; private set; }

        /// <summary>
        /// Gets the media content, if any, associated with the dialog response.
        /// </summary>
        public DialogAudioOutputStream MessageMedia { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the dialog response says the current turn should end.
        /// </summary>
        public bool TurnEndIndicated { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the dialog response says a new turn should immediately follow
        /// this one (without a user-initiated activation).
        /// </summary>
        public bool FollowupTurnIndicated { get; private set; }
    }
}
