// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace UWPVoiceAssistantSample.KwsPerformance
{
    /// <summary>
    /// Parameters for Keyword Performance Logging. Indicating the KWS stage, confirmation, and elapsed time.
    /// </summary>
    public class KeywordDetectionParams
    {
        /// <summary>
        /// Gets or sets the KWS Stage.
        /// </summary>
        public string Stage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Keyword stage was confirmed.
        /// </summary>
        public bool Confirmed { get; set; }

        public long EventFireTime { get; set; }

        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Gets or sets the time for keyword verification.
        /// </summary>
        public TimeSpan EndTime { get; set; }
    }
}
