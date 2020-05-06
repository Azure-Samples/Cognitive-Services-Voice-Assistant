// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        public string Confirmed { get; set; }

        /// <summary>
        /// Gets or sets the event fire time.
        /// </summary>
        public long EventFireTime { get; set; }

        /// <summary>
        /// Gets or sets the start time for kws.
        /// </summary>
        public long StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time for kws.
        /// </summary>
        public long EndTime { get; set; }
    }
}
