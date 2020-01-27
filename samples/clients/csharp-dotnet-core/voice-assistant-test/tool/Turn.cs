// <copyright file="Turn.cs" company="Microsoft Corporation">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representation for each turn of the dialog.
    /// </summary>
    internal class Turn
    {
        /// <summary>
        /// Gets or sets the TurnID.
        /// </summary>
        [JsonProperty(Order = -2, Required = Required.Always)]
        public int TurnID { get; set; }

        /// <summary>
        /// Gets or sets the sleep duration (in msec) before the turn begins.
        /// </summary>
        public int Sleep { get; set; }

        /// <summary>
        /// Gets or sets the Utterance.
        /// </summary>
        [JsonProperty(Order = -2, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Utterance { get; set; }

        /// <summary>
        /// Gets or sets the Activity.
        /// </summary>
        [JsonProperty(Order = -2, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Activity { get; set; }

        /// <summary>
        /// Gets or sets the WAVFile.
        /// </summary>
        [JsonProperty(Order = -2, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string WAVFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether WAVFile contains Keyword.
        /// </summary>
        [JsonProperty(Order = -2)]
        public bool Keyword { get; set; }

        /// <summary>
        /// Gets or sets the Intents.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<Tuple<string, int>> ExpectedIntents { get; set; }

        /// <summary>
        /// Gets or sets the Slots.
        /// </summary>
        [JsonProperty(Order = 1, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> ExpectedSlots { get; set; }

        /// <summary>
        /// Gets or sets the expected TTS Audio duration for audio responses from the bot.
        /// A margin defined by <see cref="AppSettings.TTSAudioDurationMargin"/> is applied to this value while validating if the actual TTS audio received matched the expected duration.
        /// </summary>
        [JsonProperty(Order = 6, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int ExpectedTTSAudioResponseDuration { get; set; }

        /// <summary>
        /// Gets or sets the List of Expected Bot Responses.
        /// </summary>
        [JsonProperty(Order = 3)]
        public List<Activity> ExpectedResponses { get; set; }

        /// <summary>
        /// Gets or sets the Maximum Latency for responses from the bot.
        /// </summary>
        [JsonProperty(Order = 8, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ExpectedResponseLatency { get; set; }
    }
}
