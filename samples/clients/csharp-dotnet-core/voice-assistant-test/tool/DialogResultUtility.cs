// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
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
        /// Gets or sets the List of turn results.
        /// </summary>
        public List<TurnResult> Turns { get; set; }

        /// <summary>
        /// Gets or sets the list of all activities received from the Bot.
        /// </summary>
        [JsonIgnore]
        public List<BotReply> BotResponses { get; set; }

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
                    CompareJObjects(expectedJObject, actualJObject, true);
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
        /// <param name="enableLogging"> Set to true to trace differences between expected and actual JObjects.</param>
        /// <returns>The count of mismatches in an activity.</returns>
        public static int CompareJObjects(JObject expected, JObject actual, bool enableLogging = false)
        {
            foreach (KeyValuePair<string, JToken> expectedPair in expected)
            {
                if (expectedPair.Value.Type == JTokenType.Object)
                {
                    if (actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase) == null)
                    {
                        ActivityMismatchCount++;
                        if (enableLogging)
                        {
                            Trace.TraceInformation($"Activity field \"{expectedPair.Key}\" not found in bot response activity.");
                        }
                    }
                    else if (actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase).Type != JTokenType.Object)
                    {
                        ActivityMismatchCount++;
                        if (enableLogging)
                        {
                            Trace.TraceInformation($"Activity field \"{expectedPair.Key}\" is not an object in bot response activity.");
                        }
                    }
                    else
                    {
                        CompareJObjects(
                            expectedPair.Value.ToObject<JObject>(),
                            actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase).ToObject<JObject>(),
                            enableLogging);
                    }
                }
                else if (expectedPair.Value.Type == JTokenType.Array)
                {
                    if (actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase) == null)
                    {
                        ActivityMismatchCount++;
                        if (enableLogging)
                        {
                            Trace.TraceInformation($"Activity field \"{expectedPair.Key}\" not found in bot response activity.");
                        }
                    }
                    else if (actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase).Type != JTokenType.Array)
                    {
                        ActivityMismatchCount++;
                        if (enableLogging)
                        {
                            Trace.TraceInformation($"Activity field \"{expectedPair.Key}\" is not an array in bot response activity.");
                        }
                    }
                    else
                    {
                        CompareJArrays(
                            expectedPair.Value.ToObject<JArray>(),
                            actual.GetValue(expectedPair.Key, StringComparison.OrdinalIgnoreCase).ToObject<JArray>(),
                            enableLogging);
                    }
                }
                else
                {
                    JToken expectedValue = expectedPair.Value;
                    JToken actualValue = actual.SelectToken(expectedPair.Key);
                    if (actualValue == null)
                    {
                        ActivityMismatchCount++;
                        if (enableLogging)
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
                            if (enableLogging)
                            {
                                Trace.TraceInformation($"Activity field: \"{expectedPair.Key}\" has mismatching values: \"{actualValue}\" does not equal \"{expectedValue}\".");
                            }
                        }
                    }
                }
            }

            return ActivityMismatchCount;
        }

        /// <summary>
        /// Builds the output.
        /// </summary>
        /// <param name="turns"> Input Turns.</param>
        /// <param name="bootstrapMode">Boolean which defines if turn is in bootstrapping mode or not.</param>
        /// <param name="recognizedText">Recognized text from Speech Recongition.</param>
        /// <param name="recognizedKeyword">Recogized Keyword from Keyword Recognition.</param>
        /// <returns>TurnsOutput.</returns>
        public TurnResult BuildOutput(Turn turns, bool bootstrapMode, string recognizedText, string recognizedKeyword)
        {
            TurnResult turnsOutput = new TurnResult(turns)
            {
                ActualResponses = new List<Activity>(),
                ActualTTSAudioResponseDuration = new List<int>(),
            };

            turnsOutput.ActualRecognizedText = recognizedText;

            if (recognizedKeyword != null)
            {
                turnsOutput.KeywordVerified = recognizedKeyword;
            }

            foreach (BotReply botReply in this.BotResponses)
            {
                turnsOutput.ActualResponses.Add(botReply.Activity);
                turnsOutput.ActualTTSAudioResponseDuration.Add(botReply.TTSAudioDuration);
            }

            if (bootstrapMode)
            {
                // In bootstrapping mode, ExpectedResponses field does not exist (or is null), and ExpectedResponseLatency does not exist
                if (this.BotResponses.Count > 0)
                {
                    // Report the latency of the last bot response
                    turnsOutput.ActualResponseLatency = this.BotResponses[this.BotResponses.Count - 1].Latency;
                }
            }
            else
            {
                // In normal mode, ExpectedResponses exists with one or more activities. ExpectedResponseLatency may or may not exist
                int activityIndexForLatency = 0;

                if (string.IsNullOrWhiteSpace(turnsOutput.ExpectedResponseLatency))
                {
                    activityIndexForLatency = turns.ExpectedResponses.Count - 1;
                }
                else
                {
                    if (turnsOutput.ExpectedResponseLatency.Split(",").Length == 2)
                    {
                        // The user has specified an expected response latency in the two-integer string format "latency,index". Extract the index
                        // Note: the index has already been verified to be in the range [0, turns.ExpectedResponses.Count - 1]
                        activityIndexForLatency = int.Parse(turnsOutput.ExpectedResponseLatency.Split(",")[1], CultureInfo.CurrentCulture);
                    }
                    else
                    {
                        // The user has specified an expected response latency in the single integer string format, without an index
                        activityIndexForLatency = turns.ExpectedResponses.Count - 1;
                    }
                }

                if (activityIndexForLatency < this.BotResponses.Count)
                {
                    turnsOutput.ActualResponseLatency = this.BotResponses[activityIndexForLatency].Latency;
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
            turnResult.ResponseMatch = true;
            turnResult.UtteranceMatch = true;
            turnResult.TTSAudioResponseDurationMatch = true;
            turnResult.ResponseLatencyMatch = true;
            turnResult.Pass = true;

            int margin = this.appSettings.TTSAudioDurationMargin;

            if (!bootstrapMode)
            {
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

                if (turnResult.ExpectedTTSAudioResponseDuration != null && turnResult.ExpectedTTSAudioResponseDuration.Count != 0 && turnResult.ActualTTSAudioResponseDuration != null && turnResult.ActualTTSAudioResponseDuration.Count != 0)
                {
                    if (turnResult.ExpectedTTSAudioResponseDuration.Count > turnResult.ActualTTSAudioResponseDuration.Count)
                    {
                        turnResult.TTSAudioResponseDurationMatch = false;
                    }
                    else
                    {
                        for (int i = 0; i < turnResult.ExpectedTTSAudioResponseDuration.Count; i++)
                        {
                            if (turnResult.ExpectedTTSAudioResponseDuration[i] > 0)
                            {
                                if (turnResult.ActualTTSAudioResponseDuration[i] >= (turnResult.ExpectedTTSAudioResponseDuration[i] - margin) && turnResult.ActualTTSAudioResponseDuration[i] <= (turnResult.ExpectedTTSAudioResponseDuration[i] + margin))
                                {
                                    if (durationMatch)
                                    {
                                        turnResult.TTSAudioResponseDurationMatch = true;
                                    }
                                }
                                else
                                {
                                    Trace.TraceInformation($"Actual TTS audio duration {turnResult.ActualTTSAudioResponseDuration[i]} is outside the expected range {turnResult.ExpectedTTSAudioResponseDuration[i]}+/-{margin}");
                                    durationMatch = false;
                                    turnResult.TTSAudioResponseDurationMatch = false;
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(turnResult.ExpectedResponseLatency) && turnResult.ActualResponseLatency > 0)
                {
                    int expectedResponseLatency = int.Parse(turnResult.ExpectedResponseLatency.Split(",")[0], CultureInfo.CurrentCulture);
                    if (turnResult.ActualResponseLatency > expectedResponseLatency)
                    {
                        Trace.TraceInformation($"Actual bot response latency {turnResult.ActualResponseLatency} msec exceeds expected latency {expectedResponseLatency} msec");
                        turnResult.ResponseLatencyMatch = false;
                    }
                }
            }

            turnResult.Pass = turnResult.ResponseMatch && turnResult.UtteranceMatch && turnResult.TTSAudioResponseDurationMatch && turnResult.ResponseLatencyMatch;

            this.DisplayTestResultMessage(turnResult);

            return turnResult.Pass;
        }

        /// <summary>
        /// Method to compare Actual Responses JArray with Expected Responses JArray.
        /// </summary>
        /// <param name="expected">Expected Bot response activity.</param>
        /// <param name="actual">Bot response activity.</param>
        /// <param name="enableLogging"> Set to true to trace differences between expected and actual JArrays.</param>
        private static void CompareJArrays(JArray expected, JArray actual, bool enableLogging = false)
        {
            for (int index = 0; index < expected.Count; index++)
            {
                JToken expectedItem = expected[index];
                if (expectedItem.Type == JTokenType.Object)
                {
                    JToken actualItem = (index >= actual.Count) ? new JObject() : actual[index];
                    CompareJObjects(
                        expectedItem.ToObject<JObject>(),
                        actualItem.ToObject<JObject>(),
                        enableLogging);
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

                if (!turnResult.ResponseLatencyMatch)
                {
                    failMessage += "latency mismatch";
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
