// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System.Collections.Generic;

    /// <summary>
    /// Captures the results of the test run for a single dialog.
    /// This is serialized into an output file.
    /// </summary>
    internal class DialogResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogResult"/> class.
        /// Determines if a single-turn or multi-turn Dialog has completed.
        /// </summary>
        /// <param name="dialogID">DialogID.</param>
        /// <param name="description">Dialog description.</param>
        /// <param name="turnCompletionStatus">Bool indicates if test has completed a Dialog/Turn.</param>
        public DialogResult(string dialogID, List<bool> turnCompletionStatus)
        {
            this.DialogID = dialogID;
            this.NumOfTurns = turnCompletionStatus.Count;
            this.TurnCompletionResult = turnCompletionStatus;
            this.DialogCompletionStatus = !turnCompletionStatus.Contains(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogResult"/> class.
        /// </summary>
        public DialogResult()
        {
        }

        /// <summary>
        /// Gets or sets get or sets the Dialog ID.
        /// </summary>
        public string DialogID { get; set; }

        /// <summary>
        /// Gets or sets the Number of turns in this Dialog.
        /// </summary>
        public int NumOfTurns { get; set; }

        /// <summary>
        /// Gets or sets a list of Bool values indicating if the corresponding turns in the dialog has completed successfully.
        /// </summary>
        public List<bool> TurnCompletionResult { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Dialog has completed.
        /// </summary>
        public bool DialogCompletionStatus { get; set; }
    }
}
