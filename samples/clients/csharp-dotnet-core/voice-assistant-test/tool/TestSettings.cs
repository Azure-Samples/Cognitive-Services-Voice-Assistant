// <copyright file="TestSettings.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTest
{
    using System.Collections.Generic;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Settings object allows retrieval of values from AppSettings.json.
    /// </summary>
    internal class TestSettings
    {
        /// <summary>
        /// Gets or sets the test file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all the turns in file should be run in a single Bot Connection or a new, separate connection per turn.
        /// </summary>
        public bool SingleConnection { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to skip all tests listed in this file.
        /// </summary>
        public bool Skip { get; set; } = false;

        /// <summary>
        /// Gets or sets the List of Activities to ignore specified for each Input File.
        /// </summary>
        public List<Activity> IgnoreActivities { get; set; }
    }
}
