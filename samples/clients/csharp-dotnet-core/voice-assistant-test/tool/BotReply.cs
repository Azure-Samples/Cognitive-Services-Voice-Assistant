// <copyright file="BotReply.cs" company="Microsoft Corporation">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTest
{
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Captures a Bot Response Activity and its corresponding Latency.
    /// </summary>
    internal class BotReply
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BotReply"/> class.
        /// </summary>
        /// <param name="activity"> Bot Activity.</param>
        /// <param name="latency"> Latency of Bot Activity.</param>
        public BotReply(IActivity activity, int latency)
        {
            this.Activity = (Activity)activity;
            this.Latency = latency;
        }

        /// <summary>
        /// Gets or sets Bot Activity.
        /// </summary>
        public Activity Activity { get; set; }

        /// <summary>
        /// Gets or sets the Latency.
        /// </summary>
        public int Latency { get; set; }
    }
}
