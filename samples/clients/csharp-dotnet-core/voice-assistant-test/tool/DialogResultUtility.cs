// <copyright file="DialogResultUtility.cs" company="Microsoft Corporation">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;
    using Activity = Microsoft.Bot.Schema.Activity;

    /// <summary>
    /// Activity received from each Utterance is serialized and populated in the corresponding txt file.
    /// </summary>
    internal class DialogResultUtility
    {
        private AppSettings appSettings;

        /// <summary>
        ///  Initializes a new instance of the <see cref="DialogResultUtility"/> class.
        /// </summary>
        /// <param name="settings">Application settings.</param>
        /// <param name="dialogID">Unique dialog ID.</param>
        /// <param name="description">Description of the dialog.</param>
        public DialogResultUtility(AppSettings settings, string dialogID, string description)
        {
            this.appSettings = settings;
            this.DialogID = dialogID;
            this.Description = description;
        }

        /// <summary>
        /// Gets the DialogID.
        /// </summary>
        public string DialogID { get; private set; }

        /// <summary>
        /// Gets the Description. Optional text to describe what this dialog does.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets or sets the List of Turns.
        /// </summary>
        public List<TurnResult> Turns { get; set; }

        /// <summary>
        /// Gets or sets the count of LUIS Traces received.
        /// </summary>
        [JsonIgnore]
        public int LUISTraceCount { get; set; }

        /// <summary>
        /// Gets or sets the Intents recognized by LUIS.
        /// </summary>
        [JsonIgnore]
        public List<Tuple<string, int>> IntentHierarchy { get; set; }

        /// <summary>
        /// Gets or sets the Entities recognized by LUIS.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> Entities { get; set; }

        /// <summary>
        /// Gets or sets the list of all non-LUIS trace activities received from the Bot.
        /// </summary>
        [JsonIgnore]
        public List<BotReply> FinalResponses { get; set; }

        /// <summary>
        /// Iterates over the List of expected response and actual response Activities.
        /// Sends a single activity of Expected and Actual into the ActivitiesMatch Method.
        /// </summary>
        /// <param name="expected">List Activities from ExpectedResponse.</param>
        /// <param name="actual">List of Activities from ActualResponse.</param>
        /// <returns>Bool value indicating if expected activity matches to actual activity.</returns>
        public static bool ActivityListsMatch(List<Activity> expected, List<Activity> actual)
        {
            bool match = true;

            if (expected.Count == actual.Count)
            {
                for (int index = 0; index < expected.Count; index++)
                {
                    if (!ActivitiesMatch(expected[index], actual[index], true))
                    {
                        Trace.TraceInformation($"Expected activity at index {index} does not match actual activity");
                        match = false;
                        break;
                    }
                }
            }
            else
            {
                Trace.TraceInformation($"Failed because number of expected activities ({expected.Count}) does not match number of actual activities ({actual.Count})");
                match = false;
            }

            return match;
        }

        /// <summary>
        /// Finds all properties that are not null in the input object.
        /// </summary>
        /// <param name="obj">Activity.</param>
        /// <returns>Not Null Properties in Activity object.</returns>
        public static Dictionary<string, string> NotNullUtility(object obj)
        {
            var properties = new Dictionary<string, string>();
            if (obj == null)
            {
                return null;
            }

            foreach (var prop in obj.GetType().GetProperties())
            {
                var val = prop.GetValue(obj);

                if (val != null)
                {
                    if (!string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        properties.Add(prop.Name, val.ToString());
                    }
                }
            }

            return properties;
        }

        /// <summary>
        /// Check whether two activities match each other. All fields in the "expected"
        /// Activity must appear in the "actual" activity and have identical values. String
        /// comparison is done while ignoring case, white spaces and punctuation marks.
        /// </summary>
        /// <param name="expected">Expected activity.</param>
        /// <param name="actual">Actual activity.</param>
        /// <param name="verbose">Set to true for verbose tracing.</param>
        /// <returns>Bool value indicating if actual activity matches the expected activity.</returns>
        public static bool ActivitiesMatch(Activity expected, Activity actual, bool verbose = false)
        {
            if (actual == null && expected == null)
            {
                return true;
            }

            if (actual == null || expected == null)
            {
                return false;
            }

            var propertyMap = NotNullUtility(expected);

            foreach (var notNullField in propertyMap.Keys)
            {
                var value = propertyMap.TryGetValue(notNullField, out string expectedResult);
                var actualResultObject = actual.GetType().GetProperty(notNullField).GetValue(actual);

                if (actualResultObject == null)
                {
                    return false;
                }

                var actualResult = actualResultObject.ToString();

                var normalizedActualResult = new string(actualResult.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
                var normalizedExpectedResult = new string(expectedResult.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

                if (!normalizedExpectedResult.Equals(normalizedActualResult, StringComparison.OrdinalIgnoreCase))
                {
                    if (verbose)
                    {
                        Trace.TraceInformation($"Activity field mismatch: \"{expectedResult}\" does not match \"{actualResult}\"");
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Organizes Activities received from the Bot.
        /// Luis Traces containing Entities and Intents are added to the IntentHeirarchy and Entities Data Structures.
        /// All other activities are added to the list of FinalResponses.
        /// </summary>
        /// <param name="allActivities">Activities received from Bot.</param>
        public void OrganizeActivities(List<BotReply> allActivities)
        {
            this.LUISTraceCount = 0;
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

                        this.LUISTraceCount += 1;
                        this.IntentHierarchy.Add(new Tuple<string, int>(topScoringIntent.ToUpperInvariant(), this.LUISTraceCount));
                    }
                    else
                    {
                        // Populate other bot responses to measure task completion rates
                        this.FinalResponses.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Builds the output.
        /// </summary>
        /// <param name="turns"> Input Turns.</param>
        /// <param name="recognizedText">Recognized text from Speech Recongition.</param>
        /// <param name="recognizedKeyword">Recogized Keyword from Keyword Recognition.</param>
        /// <returns>TurnsOutput.</returns>
        public TurnResult BuildOutput(Turn turns, string recognizedText, string recognizedKeyword)
        {
            TurnResult turnsOutput = new TurnResult(turns)
            {
                ExpectedSlots = new Dictionary<string, string>(),
                ActualResponses = new List<Activity>(),
                ActualTTSAudioReponseDuration = new List<int>(),
            };

            int activityIndex = 0;

            if (turns.ExpectedResponses != null)
            {
                turnsOutput.ExpectedResponses = turns.ExpectedResponses;
                activityIndex = turns.ExpectedResponses.Count - 1;
            }

            // Actual values
            turnsOutput.ActualIntents = this.IntentHierarchy;
            turnsOutput.ActualSlots = this.Entities;

            if (recognizedKeyword != null)
            {
                turnsOutput.KeywordVerified = recognizedKeyword;
            }

            foreach (BotReply botReply in this.FinalResponses)
            {
                turnsOutput.ActualResponses.Add(botReply.Activity);
                turnsOutput.ActualTTSAudioReponseDuration.Add(botReply.TTSAudioDuration);
            }

            turnsOutput.ActualRecognizedText = recognizedText;

            if (!string.IsNullOrWhiteSpace(turnsOutput.ExpectedResponseLatency))
            {
                if (turnsOutput.ExpectedResponseLatency.Split(",").Length == 2)
                {
                    activityIndex = int.Parse(turnsOutput.ExpectedResponseLatency.Split(",")[1], CultureInfo.CurrentCulture);
                }

                if (turns.ExpectedResponses.Count == turnsOutput.ActualResponses.Count)
                {
                    if (ActivitiesMatch(turns.ExpectedResponses[activityIndex], turnsOutput.ActualResponses[activityIndex]))
                    {
                        turnsOutput.ActualResponseLatency = this.FinalResponses[activityIndex].Latency;
                    }
                }
            }

            return turnsOutput;
        }

        /// <summary>
        /// Returns bool values indicating if Actual parameters match expected parameters.
        /// Parameters include Intents, slots, responses, TTS duration, and latency.
        /// </summary>
        /// <param name="turnResult">TurnResult object capturing the responses from the bot for this turn.</param>
        /// <param name="bootstrapMode"> Boolean which defines if turn is in bootstrapping mode or not.</param>
        /// <returns>true if the all turn tests pass, false if any of the test failed.</returns>
        public bool ValidateTurn(TurnResult turnResult, bool bootstrapMode)
        {
            turnResult.IntentMatch = true;
            turnResult.SlotMatch = true;
            turnResult.ResponseMatch = true;
            turnResult.UtteranceMatch = true;
            turnResult.TTSAudioResponseDurationMatch = true;
            turnResult.ResponseLatencyMatch = true;
            turnResult.Pass = true;
            turnResult.TaskCompleted = true;

            int margin = this.appSettings.TTSAudioDurationMargin;

            if (!bootstrapMode)
            {
                if (turnResult.ExpectedIntents == null)
                {
                    // Expected intents were not specified for this dialog in the JSON file. Ignore intents validation by marking them as matched, regardless of actual intents
                    turnResult.IntentMatch = true;
                }
                else
                {
                    turnResult.IntentMatch = !turnResult.ExpectedIntents.Except(turnResult.ActualIntents).Any();
                }

                turnResult.SlotMatch = (turnResult.ActualSlots.Count == turnResult.ExpectedSlots.Count) && (!turnResult.ActualSlots.Except(turnResult.ExpectedSlots).Any());
                turnResult.ResponseMatch = DialogResultUtility.ActivityListsMatch(turnResult.ExpectedResponses, turnResult.ActualResponses);

                if (!string.IsNullOrWhiteSpace(turnResult.WAVFile))
                {
                    var normalizedActualRecognizedText = new string(turnResult.ActualRecognizedText.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
                    var normalizedExpectedRecognizedText = new string(turnResult.Utterance.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

                    if (!normalizedExpectedRecognizedText.Equals(normalizedActualRecognizedText, StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.TraceInformation($"Recognized text \"{turnResult.ActualRecognizedText}\" does not match \"{turnResult.Utterance}\"");
                        turnResult.UtteranceMatch = false;
                    }
                }

                bool durationMatch = true;

                if (turnResult.ExpectedTTSAudioResponseDuration != null && turnResult.ExpectedTTSAudioResponseDuration.Count != 0 && turnResult.ActualTTSAudioReponseDuration != null && turnResult.ActualTTSAudioReponseDuration.Count != 0)
                {
                    for (int i = 0; i < turnResult.ExpectedTTSAudioResponseDuration.Count; i++)
                    {
                        if (turnResult.ExpectedTTSAudioResponseDuration[i] > 0)
                        {
                            if (turnResult.ActualTTSAudioReponseDuration[i] >= (turnResult.ExpectedTTSAudioResponseDuration[i] - margin) && turnResult.ActualTTSAudioReponseDuration[i] <= (turnResult.ExpectedTTSAudioResponseDuration[i] + margin))
                            {
                                if (durationMatch)
                                {
                                    turnResult.TTSAudioResponseDurationMatch = true;
                                }
                            }
                            else
                            {
                                Trace.TraceInformation($"Actual TTS audio duration {turnResult.ActualTTSAudioReponseDuration[i]} is outside the expected range {turnResult.ExpectedTTSAudioResponseDuration[i]}+/-{margin}");
                                durationMatch = false;
                                turnResult.TTSAudioResponseDurationMatch = false;
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(turnResult.ExpectedResponseLatency))
                {
                    int expectedResponseLatency = int.Parse(turnResult.ExpectedResponseLatency.Split(",")[0], CultureInfo.CurrentCulture);
                    if ((turnResult.ActualResponseLatency > expectedResponseLatency) || (turnResult.ExpectedResponses.Count != turnResult.ActualResponses.Count))
                    {
                        Trace.TraceInformation($"Actual bot response latency {turnResult.ActualResponseLatency} exceeds expected latency {expectedResponseLatency}");
                        turnResult.ResponseLatencyMatch = false;
                    }
                }
            }

            turnResult.Pass = turnResult.IntentMatch && turnResult.SlotMatch && turnResult.ResponseMatch && turnResult.UtteranceMatch && turnResult.TTSAudioResponseDurationMatch && turnResult.ResponseLatencyMatch;
            turnResult.TaskCompleted = turnResult.ResponseMatch && turnResult.UtteranceMatch;

            this.DisplayTestResultMessage(turnResult);

            return turnResult.Pass;
        }

        /// <summary>
        /// Logs the test pass/fail message.
        /// </summary>
        /// <param name="turnResult">Resulting dialog turn.</param>
        private void DisplayTestResultMessage(TurnResult turnResult)
        {
            if (turnResult.Pass)
            {
                Trace.TraceInformation($"Turn passed (DialogId {this.DialogID}, TurnID {turnResult.TurnID})");
            }
            else
            {
                string failMessage = $"Turn failed (DialogId {this.DialogID}, TurnID {turnResult.TurnID}) due to: ";
                bool commaNeeded = false;
                if (!turnResult.IntentMatch)
                {
                    failMessage += "intent mismatch";
                    commaNeeded = true;
                }

                if (!turnResult.ResponseLatencyMatch)
                {
                    if (commaNeeded)
                    {
                        failMessage += ", ";
                    }

                    failMessage += "latency mismatch";
                    commaNeeded = true;
                }

                if (!turnResult.SlotMatch)
                {
                    if (commaNeeded)
                    {
                        failMessage += ", ";
                    }

                    failMessage += "slot mismatch";
                    commaNeeded = true;
                }

                if (!turnResult.ResponseMatch)
                {
                    if (commaNeeded)
                    {
                        failMessage += ", ";
                    }

                    failMessage += "bot response mismatch";
                    commaNeeded = true;
                }

                if (!turnResult.UtteranceMatch)
                {
                    if (commaNeeded)
                    {
                        failMessage += ", ";
                    }

                    failMessage += "utterance mismatch";
                    commaNeeded = true;
                }

                if (!turnResult.TTSAudioResponseDurationMatch)
                {
                    if (commaNeeded)
                    {
                        failMessage += ", ";
                    }

                    failMessage += "TTS audio duration mismatch";
                }

                Trace.TraceInformation(failMessage);
            }
        }
    }
}
