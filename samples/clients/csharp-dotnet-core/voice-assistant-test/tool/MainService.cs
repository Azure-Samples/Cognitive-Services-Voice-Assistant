// <copyright file="MainService.cs" company="Microsoft Corporation">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using VoiceAssistantTest.Resources;
    using Activity = Microsoft.Bot.Schema.Activity;

    /// <summary>
    /// Entry Point of LUIS Accuracy Score Application.
    /// </summary>
    internal class MainService
    {
        /// <summary>
        /// Main method for application execution.
        /// Obtains the appsettings to connect to a Bot. Sends the inputs specified in each row of an Input File to the bot. Aggregates the responses from the bot and writes to an output file.
        /// </summary>
        /// <param name="configFile">App level config file.</param>
        /// <returns>Returns 0 if all tests executed successfully.</returns>
        public static async Task<int> StartUp(string configFile)
        {
            // Set default configuration for application tracing
            InitializeTracing();

            if (!CheckNotNullNotEmptyString(configFile))
            {
                // AppSettings File not specified
                throw new ArgumentException(ErrorStrings.CONFIG_FILE_MISSING);
            }

            var appSettings = AppSettings.Load(configFile);

            // Adjust application tracing based on loaded settings
            ConfigureTracing(appSettings.AppLogEnabled, appSettings.OutputFolder);

            // Validating the test files
            ValidateTestFiles(appSettings);

            // Processing the test files
            await ProcessTestFiles(appSettings).ConfigureAwait(false);

            return 0;
        }

        /// <summary>
        /// Validating each test file specified in the App Configuration file.
        /// </summary>
        /// <param name="appSettings"> Application settings.</param>
        private static void ValidateTestFiles(AppSettings appSettings)
        {
            // Validation loop running separately
            List<string> allExceptions = new List<string>
            {
                "\n",
            };

            bool noTestFilesForProcessing = true;

            foreach (TestSettings tests in appSettings.Tests)
            {
                if (tests.Skip)
                {
                    continue;
                }
                else
                {
                    noTestFilesForProcessing = false;
                }

                string inputFileName = appSettings.InputFolder + tests.FileName;

                if (!File.Exists(inputFileName))
                {
                    allExceptions.Add($"[{inputFileName}] : {ErrorStrings.FILE_DOES_NOT_EXIST}");
                    continue;
                }

                Trace.TraceInformation($"Validating file {tests.FileName}");
                StreamReader file = new StreamReader(inputFileName, Encoding.UTF8);
                string txt = file.ReadToEnd();
                file.Close();

                List<Dialog> fileContents = new List<Dialog>();
                try
                {
                    fileContents = JsonConvert.DeserializeObject<List<Dialog>>(txt);

                    ValidateUniqueDialogID(fileContents);
                }
                catch (Exception e)
                {
                    // This will throw a JSONException in case of deserialization issues, which should capture the source (such as a malformed string)
                    // There is no more processing that can be done for this file - move on to the next file
                    allExceptions.Add($"[{tests.FileName}] : {e.Message}");
                    continue;
                }

                bool firstDialog = true;

                foreach (Dialog dialog in fileContents)
                {
                    bool firstTurn = true;
                    int turnIndex = 0;

                    foreach (Turn turn in dialog.Turns)
                    {
                        ValidateTurnID(turn, turnIndex);
                        (bool valid, List<string> turnExceptionMessages) = ValidateTurnInput(turn, appSettings.BotGreeting, tests.SingleConnection, firstDialog, firstTurn);

                        if (!valid)
                        {
                            List<string> completeExceptionMsg = turnExceptionMessages.Select(str => $"[{tests.FileName}][DialogID {dialog.DialogID}, TurnID {turn.TurnID}] : {str}").ToList();
                            allExceptions.AddRange(completeExceptionMsg);
                        }

                        firstDialog = false;
                        firstTurn = false;
                        turnIndex++;
                    }
                }
            }

            if (noTestFilesForProcessing)
            {
                allExceptions.Add(ErrorStrings.ALL_TESTFILES_SKIPPED);
            }

            if (allExceptions.Count > 1)
            {
                // There are validation errors - throw exception
                string msg = string.Join("\n", allExceptions);
                throw new ArgumentException(msg);
            }
        }

        private static async Task ProcessTestFiles(AppSettings appSettings)
        {
            List<TestReport> allInputFilesTestReport = new List<TestReport>();

            foreach (TestSettings tests in appSettings.Tests)
            {
                bool isFirstDialog = true;
                bool connectionEstablished = false;
                BotConnector botConnector = null;
                Trace.IndentLevel = 0;
                if (tests.Skip)
                {
                    Trace.TraceInformation($"Skipping file {tests.FileName}");
                    continue;
                }
                else
                {
                    Trace.TraceInformation($"Processing file {tests.FileName}");
                }

                string inputFileName = appSettings.InputFolder + tests.FileName;
                string testName = Path.GetFileNameWithoutExtension(inputFileName);
                string outputPath = appSettings.OutputFolder + testName + "Output";
                DirectoryInfo outputDirectory = Directory.CreateDirectory(outputPath);

                string outputFileName = Path.Combine(outputDirectory.FullName, testName + "Output.txt");

                StreamReader file = new StreamReader(inputFileName, Encoding.UTF8);
                string txt = file.ReadToEnd();
                file.Close();

                var fileContents = JsonConvert.DeserializeObject<List<Dialog>>(txt);

                // Keep track of results for a single input file. This variable is written as the output of the test run for this input file.
                List<DialogResultUtility> testFileResults = new List<DialogResultUtility>();

                // Keep track of dialog results : per dialog.
                List<DialogResult> dialogResults = new List<DialogResult>();

                foreach (Dialog dialog in fileContents)
                {
                    Trace.IndentLevel = 1;
                    if (dialog.Skip)
                    {
                        Trace.TraceInformation($"Skipping DialogID {dialog.DialogID}");
                        continue;
                    }
                    else
                    {
                        Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Running DialogId {dialog.DialogID}, description \"{dialog.Description}\"");
                    }

                    // Capture and compute the output for this dialog in this variable.
                    DialogResultUtility dialogOutput = new DialogResultUtility(appSettings, dialog.DialogID, dialog.Description);

                    // Capture outputs of all turns in this dialog in this list.
                    List<TurnResult> turnResults = new List<TurnResult>();

                    // Keep track of turn pass/fail : per turn.
                    List<bool> turnCompletionStatuses = new List<bool>();

                    if (isFirstDialog || tests.SingleConnection == false)
                    {
                        // Always establish a connection with the bot for the first dialog in the test file.
                        // If SingleConnection is false, it also means we need to re-establish the connection before each of the dialogs in the test file.
                        if (botConnector != null)
                        {
                            await botConnector.Disconnect().ConfigureAwait(false);
                            botConnector.Dispose();
                        }

                        connectionEstablished = true;
                        botConnector = new BotConnector();
                        botConnector.InitConnector(appSettings);
                        await botConnector.Connect().ConfigureAwait(false);
                    }

                    isFirstDialog = false;

                    foreach (Turn turn in dialog.Turns)
                    {
                        // Application crashes in a multi-turn dialog with Keyword in each Turn
                        // Crash occurs when calling StartKeywordRecognitionAsync after calling StopKeywordRecognitionAsync in the previous Turn.
                        // In order to avoid this crash, only have Keyword in Turn 0 of a multi-turn Keyword containing Dialog.
                        // This is being investigated.
                        // MS-Internal bug number: 2300634.
                        // https://msasg.visualstudio.com/Skyman/_workitems/edit/2300634/
                        if (turn.Keyword)
                        {
                            await botConnector.StartKeywordRecognitionAsync().ConfigureAwait(false);
                        }

                        Trace.IndentLevel = 2;
                        Trace.TraceInformation($"[{DateTime.Now.ToString("h:mm:ss tt", CultureInfo.CurrentCulture)}] Running Turn {turn.TurnID}");
                        Trace.IndentLevel = 3;

                        if (turn.Sleep > 0)
                        {
                            Trace.TraceInformation($"Sleeping for {turn.Sleep} msec");
                            System.Threading.Thread.Sleep(turn.Sleep);
                        }

                        int responseCount = 0;
                        bool bootstrapMode = true;

                        if (turn.ExpectedResponses != null && turn.ExpectedResponses.Count != 0)
                        {
                            responseCount = turn.ExpectedResponses.Count;
                            bootstrapMode = false;
                        }

                        botConnector.SetInputValues(turn.Utterance, testName, dialog.DialogID, turn.TurnID, responseCount, tests.IgnoreActivities, turn.ExpectedResponseLatency, turn.Keyword);

                        // Send up WAV File if present
                        if (!string.IsNullOrEmpty(turn.WAVFile))
                        {
                            botConnector.SendAudio(turn.WAVFile);
                        }

                        // Send up Utterance if present
                        else if (!string.IsNullOrEmpty(turn.Utterance))
                        {
                            botConnector = await botConnector.Send(turn.Utterance).ConfigureAwait(false);
                        }

                        // Send up activity if configured
                        else if (!string.IsNullOrEmpty(turn.Activity))
                        {
                            botConnector = await botConnector.SendActivity(turn.Activity).ConfigureAwait(false);
                        }

                        // All bot reply activities are captured in this variable.
                        List<BotReply> responseActivities = botConnector.WaitAndProcessBotReplies(bootstrapMode);

                        // Separate LUIS traces and other response activities.
                        dialogOutput.OrganizeActivities(responseActivities);

                        // Capture the result of this turn in this variable and validate the turn.
                        TurnResult turnResult = dialogOutput.BuildOutput(turn, botConnector.DurationInMs, botConnector.RecognizedText, botConnector.RecognizedKeyword);
                        dialogOutput.ValidateTurn(turnResult, bootstrapMode);

                        // Add the turn result to the list of turn results.
                        turnResults.Add(turnResult);

                        // Add the turn completion status to the list of turn completions.
                        turnCompletionStatuses.Add(turnResult.TaskCompleted);

                        if (turn.Keyword)
                        {
                            await botConnector.StopKeywordRecognitionAsync().ConfigureAwait(false);
                        }
                    } // End of turns loop

                    dialogOutput.Turns = turnResults;
                    testFileResults.Add(dialogOutput);

                    DialogResult dialogResult = new DialogResult(dialogOutput.DialogID, turnCompletionStatuses);
                    dialogResults.Add(dialogResult);
                    turnCompletionStatuses = new List<bool>();

                    Trace.IndentLevel = 1;
                    if (dialogResult.DialogCompletionStatus)
                    {
                        Trace.TraceInformation($"DialogId {dialog.DialogID} passed");
                    }
                    else
                    {
                        Trace.TraceInformation($"DialogId {dialog.DialogID} failed");
                    }
                } // End of dialog loop

                TestReport fileTestReport = new TestReport
                {
                    FileName = inputFileName,
                    DialogResults = dialogResults,
                    TotalNumDialog = dialogResults.Count,
                };
                fileTestReport.ComputeTaskCompletionRate();
                allInputFilesTestReport.Add(fileTestReport);

                File.WriteAllText(outputFileName, JsonConvert.SerializeObject(testFileResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

                if (connectionEstablished)
                {
                    await botConnector.Disconnect().ConfigureAwait(false);
                    botConnector.Dispose();
                }
            } // End of inputFiles loop

            File.WriteAllText(appSettings.OutputFolder + ProgramConstants.TestReportFileName, JsonConvert.SerializeObject(allInputFilesTestReport, Formatting.Indented));
        }

        /// <summary>
        /// Initialize application tracing.
        /// By default trace to console and debug output.
        /// </summary>
        private static void InitializeTracing()
        {
            Trace.AutoFlush = true;
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        }

        /// <summary>
        /// Configure application tracing.
        /// </summary>
        /// <param name="appLogEnabled">If true, traces will be logged to the text file named LUAccuracyTesting.log.</param>
        /// <param name="outputFolder">Root output folder to write the log file.</param>
        private static void ConfigureTracing(bool appLogEnabled, string outputFolder)
        {
            if (appLogEnabled)
            {
                // Create a text writer with the given file name to trace the application
                Stream logFileStream = File.Create($"{outputFolder}VoiceAssistantTest.log");
                TextWriterTraceListener textWriterTraceListener = new TextWriterTraceListener(logFileStream);
                Trace.Listeners.Add(textWriterTraceListener);
            }
        }

        /// <summary>
        /// Check if a string is not null, not empty, and not whitespace.
        /// </summary>
        /// <param name="str">A string.</param>
        /// <returns>True if the string is not null, not empty, and not whitespace.</returns>
        private static bool CheckNotNullNotEmptyString(string str)
        {
            return !string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// Check if the string representing an activity is properly formed.
        /// </summary>
        /// <param name="activity">String serialization of an Activity.</param>
        /// <returns>True if the string represents a valid activity.</returns>
        private static (bool, string) CheckValidActivity(string activity)
        {
            Activity activityObject;
            try
            {
                activityObject = JsonConvert.DeserializeObject<Activity>(activity);
            }
            catch (Exception)
            {
                return (false, ErrorStrings.ACTIVITY_STRING_MALFORMED);
            }

            if (string.IsNullOrEmpty(activityObject.Type) || string.IsNullOrEmpty(activityObject.Type))
            {
                // No type set (type needs to be message)
                return (false, ErrorStrings.ACTIVITY_TYPE_NOT_SET);
            }

            return (true, null);
        }

        /// <summary>
        /// Checks if the Expected Latency is properly formatted.
        /// </summary>
        /// <param name="expectedLatency"> Expected Latency.</param>
        /// <param name="expectedResponseSize"> Size of Expected Response Array.</param>
        /// <returns>True if the string is formatted properly else false.<returns>
        private static bool CheckValidExpectedLatency(string expectedLatency, int expectedResponseSize)
        {
            string[] latency = expectedLatency.Split(",");
            if (latency.Length > 2)
            {
                return false;
            }
            else if (latency.Length == 2)
            {
                if (!int.TryParse(latency[1], out int parsedIndex) || (parsedIndex > expectedResponseSize) || parsedIndex < 0)
                {
                    return false;
                }
            }

            if (!int.TryParse(latency[0], out int parsedLatency) || parsedLatency < 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for proper, valid input combination of the dialog turns.
        /// </summary>
        /// <param name="turn">A turn object.</param>
        /// <param name="botGreeting">Bot greeting value from the application settings.</param>
        /// <param name="singleConnection">Single connection value from the test settings.</param>
        /// <param name="firstDialog">true if this is the first dialog in the test file.</param>
        /// <param name="firstTurn">true if this is the first turn in the dialog.</param>
        /// <returns>A tuple of a boolean and a list of strings. The boolean is set to true if the string is valid and the list of strings captures the error messages if the turn is not valid.</returns>
        private static (bool, List<string>) ValidateTurnInput(Turn turn, bool botGreeting, bool singleConnection, bool firstDialog, bool firstTurn)
        {
            bool utterancePresentValid = CheckNotNullNotEmptyString(turn.Utterance);
            bool activityPresentValid = CheckNotNullNotEmptyString(turn.Activity);
            bool wavFilePresentValid = CheckNotNullNotEmptyString(turn.WAVFile);
            bool expectedLatencyPresentValid = CheckNotNullNotEmptyString(turn.ExpectedResponseLatency);

            List<string> exceptionMessage = new List<string>();

            if (activityPresentValid)
            {
                var (activityObjectValid, errorMsg) = CheckValidActivity(turn.Activity);
                if (!activityObjectValid)
                {
                    exceptionMessage.Add(errorMsg);
                }
            }

            if (expectedLatencyPresentValid)
            {
                if (turn.ExpectedResponses != null && turn.ExpectedResponses.Count != 0)
                {
                    var expectedLatencyObjectValid = CheckValidExpectedLatency(turn.ExpectedResponseLatency, turn.ExpectedResponses.Count);
                    if (!expectedLatencyObjectValid)
                    {
                        exceptionMessage.Add(ErrorStrings.LATENCY_STRING_MALFORMED);
                    }
                }
                else
                {
                    exceptionMessage.Add(ErrorStrings.LATENCY_STRING_PRESENT);
                }
            }

            if (turn.ExpectedTTSAudioResponseDuration < 0)
            {
                exceptionMessage.Add(ErrorStrings.TTS_AUDIO_DURATION_INVALID);
            }

            if ((turn.ExpectedResponses == null || turn.ExpectedResponses.Count == 0) && turn.ExpectedTTSAudioResponseDuration > 0)
            {
                exceptionMessage.Add(ErrorStrings.TTS_AUDIO_DURATION_PRESENT);
            }

            if (utterancePresentValid && wavFilePresentValid && activityPresentValid)
            {
                exceptionMessage.Add($"{ErrorStrings.AMBIGUOUS_TURN_INPUT} - {ErrorStrings.ALL_PRESENT}");
            }

            if (wavFilePresentValid && !utterancePresentValid && activityPresentValid)
            {
                exceptionMessage.Add($"{ErrorStrings.AMBIGUOUS_TURN_INPUT} - {ErrorStrings.WAV_FILE_ACTIVITY_PRESENT}");
            }

            if (utterancePresentValid && !wavFilePresentValid && activityPresentValid)
            {
                exceptionMessage.Add($"{ErrorStrings.AMBIGUOUS_TURN_INPUT} - {ErrorStrings.UTTERANCE_ACTIVITY_PRESENT}");
            }

            // By default, assume there should be an input ("Utterance", "Activity" or "WAVFile")
            bool inputExpected = true;

            // Change the default for the case where a turn checking bot greeting is expected. For bot greeting, there should be no input.
            if (botGreeting && firstTurn && ((firstDialog && singleConnection) || !singleConnection))
            {
                inputExpected = false;
            }

            // Does the current dialog have input?
            bool inputPresent = utterancePresentValid || wavFilePresentValid || activityPresentValid;

            if (inputExpected && !inputPresent)
            {
                // There should have been an input specified ("Utterance", "Activity" or "WAVFile"), but there are none. That's an error.
                exceptionMessage.Add($"{ErrorStrings.AMBIGUOUS_TURN_INPUT} - {ErrorStrings.NONE_PRESENT}");
            }

            if (!inputExpected && inputPresent)
            {
                // There is an input specified ("Utterance", "Activity" or "WAVFile"), but there should be no input. That's an error
                exceptionMessage.Add(ErrorStrings.BOT_GREETING_MISSING);
            }

            if (turn.Sleep < 0)
            {
                // Sleep duration in msec - The value should be non-negative
                exceptionMessage.Add(ErrorStrings.NEGATIVE_SLEEP_DURATION);
            }

            if (exceptionMessage.Count > 0)
            {
                return (false, exceptionMessage);
            }

            return (true, exceptionMessage);
        }

        private static void ValidateUniqueDialogID(List<Dialog> testValues)
        {
            List<string> uniqueDialog = new List<string>();
            foreach (var item in testValues)
            {
                uniqueDialog.Add(item.DialogID);
            }

            if (uniqueDialog.GroupBy(x => x).Any(y => y.Count() > 1))
            {
                throw new ArgumentException(ErrorStrings.DUPLICATE_DIALOGID);
            }
        }

        private static void ValidateTurnID(Turn turn, int turnIndex)
        {
            if (turn.TurnID < 0)
            {
                throw new ArgumentException(ErrorStrings.NEGATIVE_TURNID);
            }

            if (turn.TurnID != turnIndex)
            {
                throw new ArgumentException(ErrorStrings.INVALID_TURNID_SEQUENCE);
            }
        }
    }
}
