// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
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
        /// Gets or sets a value indicating whether the trace has to be shown or not.
        /// </summary>
        public static bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets a ActivityMismatchCount.
        /// </summary>
        public static int ActivityMismatchCount { get; set; } = 0;

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
                    ActivityMismatchCount = 0;
                    string expectedSerializedJson = JsonConvert.SerializeObject(expected[index], new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    string actualSerializedJson = JsonConvert.SerializeObject(actual[index], new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    JObject expectedJObject = JsonConvert.DeserializeObject<JObject>(expectedSerializedJson);
                    JObject actualJObject = JsonConvert.DeserializeObject<JObject>(actualSerializedJson);
                    CompareJObjects(expectedJObject, actualJObject);
                    if (ActivityMismatchCount > 0)
                    {
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
        /// Check whether two activities match each other. All fields in the "expected"
        /// Activity must appear in the "actual" activity and have identical values. String
        /// comparison is done while ignoring case, white spaces and punctuation marks.
        /// </summary>
        /// <param name="expected"> Expected Bot response activity. </param>
        /// <param name="actual"> Bot response activity. </param>
        /// <returns>The count of mismatchs in an activity.</returns>
        public static int CompareJObjects(JObject expected, JObject actual)
        {
            foreach (KeyValuePair<string, JToken> expectedPair in expected)
            {
                if (expectedPair.Value.Type == JTokenType.Object)
                {
                    if (actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase) == null)
                    {
                        ActivityMismatchCount++;
                        if (Verbose)
                        {
                            Trace.TraceInformation($"Activity field \"{expectedPair.Key}\" not found in bot response activity.");
                        }
                    }
                    else if (actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase).Type != JTokenType.Object)
                    {
                        ActivityMismatchCount++;
                        if (Verbose)
                        {
                            Trace.TraceInformation($"Activity field \"{expectedPair.Key}\" is not an object in bot response activity.");
                        }
                    }
                    else
                    {
                        CompareJObjects(
                            expectedPair.Value.ToObject<JObject>(),
                            actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase).ToObject<JObject>());
                    }
                }
                else if (expectedPair.Value.Type == JTokenType.Array)
                {
                    if (actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase) == null)
                    {
                        ActivityMismatchCount++;
                        if (Verbose)
                        {
                            Trace.TraceInformation($"Activity field \"{expectedPair.Key}\" not found in bot response activity.");
                        }
                    }
                    else if (actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase).Type != JTokenType.Array)
                    {
                        ActivityMismatchCount++;
                        if (Verbose)
                        {
                            Trace.TraceInformation($"Activity field \"{expectedPair.Key}\" is not an array in bot response activity.");
                        }
                    }
                    else
                    {
                        CompareJArrays(
                            expectedPair.Value.ToObject<JArray>(),
                            actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase).ToObject<JArray>());
                    }
                }
                else
                {
                    JToken expectedValue = expectedPair.Value;
                    JToken actualValue = actual.SelectToken(expectedPair.Key);
                    if (actualValue == null)
                    {
                        ActivityMismatchCount++;
                        if (Verbose)
                        {
                            Trace.TraceInformation($"Activity field \"{expectedPair.Key}\" not found in bot response activity.");
                        }
                    }
                    else
                    {
                        string actualResult = actualValue.ToString();
                        string expectedResult = expectedValue.ToString();

                        string normalizedActualResult = new string(actualResult.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
                        string normalizedExpectedResult = new string(expectedResult.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

                        if (!normalizedExpectedResult.Equals(normalizedActualResult, StringComparison.OrdinalIgnoreCase))
                        {
                            ActivityMismatchCount++;
                            if (Verbose)
                            {
                                Trace.TraceInformation($"Activity field: \" {expectedPair.Key} \", has mismatching values. \"{actualValue}\",\"{expectedValue}\".");
                            }
                        }
                    }
                }
            }

            return ActivityMismatchCount;
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

            foreach (BotReply item in allActivities)
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
                        string topScoringIntent = JsonConvert.DeserializeObject<Dictionary<string, object>>(lUISResult["topScoringIntent"].ToString())["intent"].ToString();
                        string entityString = lUISResult["entities"]?.ToString();
                        if (entityString == "[]")
                        {
                            entityString = string.Empty;
                        }

                        List<Dictionary<string, object>> entities = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(entityString);

                        if (entities != null)
                        {
                            foreach (Dictionary<string, object> entityDict in entities)
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
                    turnsOutput.ActualResponseLatency = this.FinalResponses[activityIndex].Latency;
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
                    string normalizedActualRecognizedText = new string(turnResult.ActualRecognizedText.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
                    string normalizedExpectedRecognizedText = new string(turnResult.Utterance.Where(c => !char.IsPunctuation(c) && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

                    if (!normalizedExpectedRecognizedText.Equals(normalizedActualRecognizedText, StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.TraceInformation($"Recognized text \"{turnResult.ActualRecognizedText}\" does not match \"{turnResult.Utterance}\"");
                        turnResult.UtteranceMatch = false;
                    }
                }

                bool durationMatch = true;

                if (turnResult.ExpectedTTSAudioResponseDuration != null && turnResult.ExpectedTTSAudioResponseDuration.Count != 0 && turnResult.ActualTTSAudioReponseDuration != null && turnResult.ActualTTSAudioReponseDuration.Count != 0)
                {
                    if (turnResult.ExpectedTTSAudioResponseDuration.Count > turnResult.ActualTTSAudioReponseDuration.Count)
                    {
                        turnResult.TTSAudioResponseDurationMatch = false;
                    }
                    else
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
        /// Method to compare Actual Responses JArray with Expected Responses JArray.
        /// </summary>
        /// <param name="expected">Expected Bot response activity.</param>
        /// <param name="actual">Bot response activity.</param>
        private static void CompareJArrays(JArray expected, JArray actual)
        {
            for (int index = 0; index < expected.Count; index++)
            {
                JToken expectedItem = expected[index];
                if (expectedItem.Type == JTokenType.Object)
                {
                    JToken actualItem = (index >= actual.Count) ? new JObject() : actual[index];
                    CompareJObjects(
                        expectedItem.ToObject<JObject>(),
                        actualItem.ToObject<JObject>());
                }
            }

            return;
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
