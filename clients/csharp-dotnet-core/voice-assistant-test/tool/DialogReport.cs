// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System.Collections.Generic;

    /// <summary>
    /// A short summary report of the dialog test run.
    /// </summary>
    internal class DialogReport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogReport"/> class.
        /// Determines if a single-turn or multi-turn Dialog has completed.
        /// </summary>
        /// <param name="dialogID">DialogID.</param>
        /// <param name="description">Dialog description.</param>
        /// <param name="turnPassResults">A list of bool values indicating turn pass (true) or failed (false) for each turn in the dialog.</param>
        public DialogReport(string dialogID, string description, List<bool> turnPassResults)
        {
            this.DialogID = dialogID;
            this.Description = description;
            this.TurnCount = turnPassResults.Count;
            this.TurnPassResults = turnPassResults;
            this.DialogPass = !turnPassResults.Contains(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogReport"/> class.
        /// </summary>
        public DialogReport()
        {
        }

        /// <summary>
        /// Gets or sets get or sets the Dialog ID.
        /// </summary>
        public string DialogID { get; set; }

        /// <summary>
        /// Gets or sets the Description. Optional text to describe what this dialog does.
        /// </summary>
        public string Description { get; set; }

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
