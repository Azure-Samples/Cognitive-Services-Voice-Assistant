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
        /// Builds the output.
        /// </summary>
        /// <param name="turns"> Input Turns.</param>
        /// <param name="intents"> Actual Intents.</param>
        /// <param name="slots">Actual Slots.</param>
        /// <param name="response">Actual Bot Responses.</param>
        /// <param name="responseDuration">Actual duration of the TTS audio.</param>
        /// <param name="recognizedText">Recognized text from Speech Recongition.</param>
        /// <param name="recognizedKeyword">Recogized Keyword from Keyword Recognition.</param>
        /// <returns>TurnsOutput.</returns>
        public TurnResult BuildOutput(Turn turns, List<Tuple<string, int>> intents, Dictionary<string, string> slots, List<BotReply> response, int responseDuration, string recognizedText, string recognizedKeyword)
        {
            TurnResult turnsOutput = new TurnResult(turns)
            {
                ExpectedSlots = new Dictionary<string, string>(),
                ActualResponses = new List<Activity>(),
            };

            int activityIndex = 0;

            if (turns.ExpectedResponses != null)
            {
                turnsOutput.ExpectedResponses = turns.ExpectedResponses;
                activityIndex = turns.ExpectedResponses.Count - 1;
            }

            // Actual values
            turnsOutput.ActualIntents = intents;
            turnsOutput.ActualSlots = slots;

            if (recognizedKeyword != null)
            {
                turnsOutput.KeywordVerified = recognizedKeyword;
            }

            foreach (BotReply botReply in response)
            {
                turnsOutput.ActualResponses.Add(botReply.Activity);
            }

            turnsOutput.ActualTTSAudioReponseDuration = responseDuration;
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
                        turnsOutput.ActualResponseLatency = response[activityIndex].Latency;
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
        public void ValidateTurn(TurnResult turnResult, bool bootstrapMode)
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
                    if (!GetStringUsingRegex(turnResult.ActualRecognizedText).Equals(GetStringUsingRegex(turnResult.Utterance), StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.TraceInformation($"Recognized text \"{turnResult.ActualRecognizedText}\" does not match \"{turnResult.Utterance}\"");
                        turnResult.UtteranceMatch = false;
                    }
                }

                if (turnResult.ExpectedTTSAudioResponseDuration > 0)
                {
                    int expectedResponseDuration = turnResult.ExpectedTTSAudioResponseDuration;
                    if (turnResult.ActualTTSAudioReponseDuration >= (expectedResponseDuration - margin) && turnResult.ActualTTSAudioReponseDuration <= (expectedResponseDuration + margin))
                    {
                        turnResult.TTSAudioResponseDurationMatch = true;
                    }
                    else
                    {
                        Trace.TraceInformation($"Actual TTS audio duration {turnResult.ActualTTSAudioReponseDuration} is outside the expected range {expectedResponseDuration}+/-{margin}");
                        turnResult.TTSAudioResponseDurationMatch = false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(turnResult.ExpectedResponseLatency))
                {
                    if ((turnResult.ActualResponseLatency > int.Parse(turnResult.ExpectedResponseLatency.Split(",")[0], CultureInfo.CurrentCulture)) || (turnResult.ExpectedResponses.Count != turnResult.ActualResponses.Count))
                    {
                        turnResult.ResponseLatencyMatch = false;
                    }
                }
            }

            turnResult.Pass = turnResult.IntentMatch && turnResult.SlotMatch && turnResult.ResponseMatch && turnResult.UtteranceMatch && turnResult.TTSAudioResponseDurationMatch && turnResult.ResponseLatencyMatch;
            turnResult.TaskCompleted = turnResult.ResponseMatch && turnResult.UtteranceMatch;

            this.DisplayTestResultMessage(turnResult);
        }

        private static string GetStringUsingRegex(string originalString)
        {
            string validString;
            validString = System.Text.RegularExpressions.Regex.Replace(originalString, @"[^\w]", string.Empty).ToLower(CultureInfo.CurrentCulture);
            return validString;
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
