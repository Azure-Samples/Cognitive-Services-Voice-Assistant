// <copyright file="DialogData.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTestGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// This class stores data associated with a Dialog.
    /// </summary>
    public class DialogData
    {
        private string description = string.Empty;
        private bool skip = false;
        private int turnID = 0;
        private int sleep = 0;
        private string wavFile = string.Empty;
        private string utterance = string.Empty;
        private string activity = string.Empty;
        private bool keyword = false;

        /// <summary>
        /// Gets or sets The Description of the dialog.
        /// </summary>
        public string Description { get => this.description; set => this.description = value; }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog should be skipped.
        /// </summary>
        public bool Skip { get => this.skip; set => this.skip = value; }

        /// <summary>
        /// Gets or sets The TurnID of the dialog.
        /// </summary>
        public int TurnID { get => this.turnID; set => this.turnID = value; }

        /// <summary>
        /// Gets or sets a value indicating how long to sleep before beginning the Dialog.
        /// </summary>
        public int Sleep { get => this.sleep; set => this.sleep = value; }

        /// <summary>
        /// Gets or sets a value indicating the WavFile to be read.
        /// </summary>
        public string WavFile { get => this.wavFile; set => this.wavFile = value; }

        /// <summary>
        /// Gets or sets a value indicating the utterance to use as a message to the service.
        /// </summary>
        public string Utterance { get => this.utterance; set => this.utterance = value; }

        /// <summary>
        /// Gets or sets a value indicating the activity to send to the service.
        /// </summary>
        public string Activity { get => this.activity; set => this.activity = value; }

        /// <summary>
        /// Gets or sets a value indicating whether we should use keyword listening or not.
        /// </summary>
        public bool Keyword { get => this.keyword; set => this.keyword = value; }
    }
}
