// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Captures a Bot Response Activity and its corresponding Latency.
    /// </summary>
    internal class BotReply
    {
        private const int DefaultTTSAudioDuration = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotReply"/> class.
        /// </summary>
        /// <param name="activity"> Bot Activity.</param>
        /// <param name="latency"> Latency of Bot Activity.</param>
        /// <param name="ignore"> Boolean which indicates whether a Bot activity is to be ignored or not.</param>
        /// <param name="ttsAudioDuration"> TTS Audio duration of Bot Activity.</param>
        public BotReply(IActivity activity, int latency, bool ignore, int ttsAudioDuration = DefaultTTSAudioDuration)
        {
            this.Activity = (Activity)activity;
            this.Latency = latency;
            this.TTSAudioDuration = ttsAudioDuration;
            this.Ignore = ignore;
        }

        /// <summary>
        /// Gets or sets Bot Activity.
        /// </summary>
        public Activity Activity { get; set; }

        /// <summary>
        /// Gets or sets the Latency.
        /// </summary>
        public int Latency { get; set; }

        /// <summary>
        /// Gets or sets the Latency.
        /// </summary>
        public int TTSAudioDuration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a Bot activity is to be ignored or not.
        /// </summary>
        public bool Ignore { get; set; }
    }
}
