// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System.Collections.Generic;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    /// <summary>
    /// Captures the results of the test run for a single turn in a dialog.
    /// </summary>
    internal class TurnResult : Turn
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TurnResult"/> class.
        /// </summary>
        /// <param name="turn"> turns.</param>
        public TurnResult(Turn turn)
        {
            this.TurnID = turn.TurnID;
            this.Sleep = turn.Sleep;
            this.Utterance = turn.Utterance;
            this.Activity = turn.Activity;
            this.WAVFile = turn.WAVFile;
            this.Keyword = turn.Keyword;
            this.ExpectedTTSAudioResponseDurations = turn.ExpectedTTSAudioResponseDurations;
            this.ExpectedResponses = turn.ExpectedResponses;
            this.ExpectedResponseLatency = turn.ExpectedResponseLatency;
        }

        /// <summary>
        /// Gets or sets the list of actual responses received from the bot.
        /// </summary>
        [JsonProperty(Order = 4)]
        public List<Activity> ActualResponses { get; set; }

        /// <summary>
        /// Gets or sets the text recognized from input speech.
        /// </summary>
        [JsonProperty(Order = 5, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ActualRecognizedText { get; set; }

        /// <summary>
        /// Gets or sets the Actual TTS Audio Reponse Duratio (in milliseconds).
        /// </summary>
        [JsonProperty(Order = 7, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<int> ActualTTSAudioResponseDurations { get; set; }

        /// <summary>
        /// Gets or sets the actual latency recorded for the response marked for measurement.
        /// </summary>
        [JsonProperty(Order = 9)]
        public int ActualResponseLatency { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Actual Responses match Expected Responses.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool ResponseMatch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ActualRecognizedText matches Utterance.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool UtteranceMatch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ActualTTSAudioResponseDuration matches ExpectedTTSAudioResponseDuration.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool TTSAudioResponseDurationMatch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ActualResponseLatency is less than ExpectedResponseLatency.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool ResponseLatencyMatch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Keyword was verified by Speech Service.
        /// </summary>
        [JsonProperty(Order = 11)]
        public string KeywordVerified { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether turn Passes or fails. True if IntentMatch, SlotMatch, and ResponseMatch are true.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool Pass { get; set; }
    }
}
