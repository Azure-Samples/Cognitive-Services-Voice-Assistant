// <copyright file="TurnResult.cs" company="Microsoft Corporation">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTest
{
    using System;
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
        /// <param name="turns"> turns.</param>
        public TurnResult(Turn turns)
        {
            this.TurnID = turns.TurnID;
            this.Utterance = turns.Utterance;
            this.Activity = turns.Activity;
            this.WAVFile = turns.WAVFile;
            this.Keyword = turns.Keyword;
            this.ExpectedIntents = turns.ExpectedIntents;
            this.ExpectedTTSAudioResponseDuration = turns.ExpectedTTSAudioResponseDuration;
            this.ExpectedResponseLatency = turns.ExpectedResponseLatency;
        }

        /// <summary>
        /// Gets or sets the actual intents obtained from LUIS traces.
        /// </summary>
        [JsonProperty(Order = 0, NullValueHandling = NullValueHandling.Ignore)]
        public List<Tuple<string, int>> ActualIntents { get; set; }

        /// <summary>
        /// Gets or sets the actual slots obtained from LUIS traces.
        /// </summary>
        [JsonProperty(Order = 2, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> ActualSlots { get; set; }

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
        public int ActualTTSAudioReponseDuration { get; set; }

        /// <summary>
        /// Gets or sets the actual latency recorded for the response marked for measurement.
        /// </summary>
        [JsonProperty(Order = 9, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int ActualResponseLatency { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Actual Intents match Expected Intents.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool IntentMatch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Actual Slots match Expected Slots.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool SlotMatch { get; set; }

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
        /// Gets or sets a value indicating whether ActualTTSAudioReponseDuration matches ExpectedTTSAudioResponseDuration.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool TTSAudioResponseDurationMatch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ActualResponseLatency is less than ExpectedResponseLatency.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool ResponseLatencyMatch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Keyword was verfied by Speech Service.
        /// </summary>
        [JsonProperty(Order = 11)]
        public string KeywordVerified { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether turn Passes or fails. True if IntentMatch, SlotMatch, and ResponseMatch are true.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool Pass { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Task is completed. True if ResponseMatch is true.
        /// </summary>
        [JsonProperty(Order = 11)]
        public bool TaskCompleted { get; set; }
    }
}
