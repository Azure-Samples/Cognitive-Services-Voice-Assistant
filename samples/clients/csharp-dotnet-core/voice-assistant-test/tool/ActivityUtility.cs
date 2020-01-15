// <copyright file="ActivityUtility.cs" company="Microsoft Corporation">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Separates LUIS traces and other bot reply activities, populates the recognized intents, slots, and other bot responses.
    /// </summary>
    internal class ActivityUtility
    {
        /// <summary>
        /// Variable to store the count of LUIS Traces received.
        /// </summary>
        private int lUISTraceCount;

        /// <summary>
        /// Gets or sets the Intents recognized by LUIS.
        /// </summary>
        public List<Tuple<string, int>> IntentHierarchy { get; set; }

        /// <summary>
        /// Gets or sets the Entities recognized by LUIS.
        /// </summary>
        public Dictionary<string, string> Entities { get; set; }

        /// <summary>
        /// Gets or sets the list of all non-LUIS trace activities received from the Bot.
        /// </summary>
        public List<BotReply> FinalResponses { get; set; }

        /// <summary>
        /// Organizes Activities received from the Bot.
        /// Luis Traces containing Entities and Intents are added to the IntentHeirarchy and Entities Data Structures.
        /// All other activities are added to the list of FinalResponses.
        /// </summary>
        /// <param name="allActivities">Activities received from Bot.</param>
        /// <returns>List of Response Activities.</returns>
        public ActivityUtility OrganizeActivities(List<BotReply> allActivities)
        {
            this.lUISTraceCount = 0;
            this.IntentHierarchy = new List<Tuple<string, int>>();
            this.Entities = new Dictionary<string, string>();
            this.FinalResponses = new List<BotReply>();

            foreach (var item in allActivities)
            {
                if (item?.Activity != null)
                {
                    // Strip LUIS Trace Activities to validate intents and slots
                    if (item?.Activity.Label == "Luis Trace")
                    {
                        // The Value Field has a Recognizer Result Summary
                        string traceResult = item.Activity.Value.ToString();
                        Dictionary<string, object> recognizerResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(traceResult);
                        Dictionary<string, object> lUISResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(recognizerResult["luisResult"].ToString());
                        var topScoringIntent = JsonConvert.DeserializeObject<Dictionary<string, object>>(lUISResult["topScoringIntent"].ToString())["intent"].ToString();
                        var entityString = lUISResult["entities"]?.ToString();
                        if (entityString == "[]")
                        {
                            entityString = string.Empty;
                        }

                        var entities = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(entityString);

                        if (entities != null)
                        {
                            foreach (var entityDict in entities)
                            {
                                if (entityDict.TryGetValue("type", out object typeKey))
                                {
                                    string key = typeKey.ToString().ToUpperInvariant();
                                    if (entityDict.TryGetValue("entity", out object entityKey))
                                    {
                                        string value = entityKey.ToString().ToUpperInvariant();
                                        this.Entities.TryAdd(key, value);
                                    }
                                }
                            }
                        }

                        this.lUISTraceCount += 1;
                        this.IntentHierarchy.Add(new Tuple<string, int>(topScoringIntent.ToUpperInvariant(), this.lUISTraceCount));
                    }
                    else
                    {
                        // Populate other bot responses to measure task completion rates
                        this.FinalResponses.Add(item);
                    }
                }
            }

            return this;
        }
    }
}
