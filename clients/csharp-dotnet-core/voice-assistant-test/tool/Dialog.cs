// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representation for each Dialog defined in the JSON input file.
    /// </summary>
    internal class Dialog
    {
        /// <summary>
        /// Gets or sets the DialogID. A unique value identifying the dialog.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DialogID { get; set; }

        /// <summary>
        /// Gets or sets the Description. Optional text to describe what this dialog does.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the List of Turns for this dialog.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<Turn> Turns { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip this dailog in the test run.
        /// </summary>
        public bool Skip { get; set; } = false;

        public string InputType { get; set; }
    }
}
