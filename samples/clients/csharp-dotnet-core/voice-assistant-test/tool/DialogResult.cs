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
        /// <param name="turnPassResults">A list of bool values indicating turn pass (true) or failed (false) for each turn in the dialog.</param>
        public DialogResult(string dialogID, List<bool> turnPassResults)
        {
            this.DialogID = dialogID;
            this.TurnCount = turnPassResults.Count;
            this.TurnPassResults = turnPassResults;
            this.DialogPass = !turnPassResults.Contains(false);
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
        /// Gets or sets the number of turns in this dialog.
        /// </summary>
        public int TurnCount { get; set; }

        /// <summary>
        /// Gets or sets a list of bool values indicating turn pass (true) or failed (false) for each turn in the dialog.
        /// </summary>
        public List<bool> TurnPassResults { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Dialog test has passed.
        /// </summary>
        public bool DialogPass { get; set; }
    }
}
