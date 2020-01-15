// <copyright file="Dialog.cs" company="Microsoft Corporation">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTest
{
    using System.Collections.Generic;

    /// <summary>
    /// Class representation for each Dialog defined in the JSON input file.
    /// </summary>
    internal class Dialog
    {
        /// <summary>
        /// Gets or sets the DialogID. A unique value identifying the dialog.
        /// </summary>
        public string DialogID { get; set; }

        /// <summary>
        /// Gets or sets the Description. Optional text to describe what this dialog does.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the List of Turns for this dialog.
        /// </summary>
        public List<Turn> Turns { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip this dailog in the test run.
        /// </summary>
        public bool Skip { get; set; } = false;
    }
}
