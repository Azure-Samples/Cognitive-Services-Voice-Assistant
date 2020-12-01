// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using VoiceAssistantTest.Resources;
    using Activity = Microsoft.Bot.Schema.Activity;

    // Define this flag to use Microsoft Aria events. It will require adding the file "AriaLogger.cs" to the project. For more info on Aria, visit http://www.aria.ms
    // #define USE_ARIA_LOGGING

    /// <summary>
    /// Entry point of Voice Assistant regression tests.
    /// </summary>
    internal class MainService
    {
        /// <summary>
        /// Main method for application execution.
        /// Obtains the appsettings to connect to a Bot. Sends the inputs specified in each row of an Input File to the bot. Aggregates the responses from the bot and writes to an output file.
        /// </summary>
        /// <param name="configFile">App level config file.</param>
        /// <returns>Returns ture if all tests executed successfully.</returns>
        public static async Task<bool> StartUp(string configFile)
        {
            // Set default configuration for application tracing
            InitializeTracing();

            if (!CheckNotNullNotEmptyString(configFile))
            {
                // AppSettings File not specified
                throw new ArgumentException(ErrorStrings.CONFIG_FILE_MISSING);
            }

            AppSettings appSettings = AppSettings.Load(configFile);

            // Adjust application tracing based on loaded settings
            ConfigureTracing(appSettings.AppLogEnabled, appSettings.OutputFolder);

            // Validating the test files
            ValidateTestFiles(appSettings);

            // Processing the test files
            return await ProcessTestFiles(appSettings).ConfigureAwait(false);
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

                if (string.IsNullOrEmpty(appSettings.InputFolder))
                {
                    appSettings.InputFolder = Directory.GetCurrentDirectory();
                }

                string inputFileName = Path.Combine(appSettings.InputFolder, tests.FileName);

                if (Path.IsPathRooted(tests.FileName))
                {
                    throw new ArgumentException($"{ErrorStrings.FILENAME_PATH_NOT_RELATIVE} - {tests.FileName}");
                }

                if (!File.Exists(inputFileName))
                {
                    allExceptions.Add($"{ErrorStrings.FILE_DOES_NOT_EXIST} - {inputFileName}");
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

        private static async Task<bool> ProcessTestFiles(AppSettings appSettings)
        {
            List<TestReport> allInputFilesTestReport = new List<TestReport>();
            bool testPass = true;

            if (!string.IsNullOrEmpty(appSettings.AriaProjectKey))
            {
#if USE_ARIA_LOGGING
                AriaLogger.Start(appSettings.AriaProjectKey);
#endif
            }

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

                string inputFileName = Path.Combine(appSettings.InputFolder, tests.FileName);
                string testName = Path.GetFileNameWithoutExtension(inputFileName);

                if (string.IsNullOrEmpty(appSettings.OutputFolder))
                {
                    appSettings.OutputFolder = Directory.GetCurrentDirectory();
                }

                string outputPath = string.Empty;
                string outputFileName = string.Empty;

                StreamReader file = new StreamReader(inputFileName, Encoding.UTF8);
                string txt = file.ReadToEnd();
                file.Close();

                var fileContents = JsonConvert.DeserializeObject<List<Dialog>>(txt);

                // Keep track of the detailed dialog results for all dialogs in a single input test file. This list will be serialized to JSON as the output for this test file.
                List<DialogResult> dialogResults = new List<DialogResult>();

                // Keep track of high-level (summary) results for all dialogs in a single input test file. This list will be serialized to JSON as part of the overall single test report.
                List<DialogReport> dialogReports = new List<DialogReport>();

                string outputType = string.Empty;
                if (tests.WavAndUtterancePairs)
                {
                    outputType = "Output-Wav";
                    await ProcessDialogAndGenerateReport(outputType, outputPath, testPass, botConnector, dialogReports, connectionEstablished, dialogResults, fileContents, isFirstDialog, tests, testName, true, inputFileName, allInputFilesTestReport, outputFileName, appSettings).ConfigureAwait(false);

                    outputType = "Output-Text";

                    await ProcessDialogAndGenerateReport(outputType, outputPath, testPass, botConnector, dialogReports, connectionEstablished, dialogResults, fileContents, isFirstDialog, tests, testName, false, inputFileName, allInputFilesTestReport, outputFileName, appSettings).ConfigureAwait(false);
                }

                // WavAndUtterancePair is false.
                else
                {
                    outputType = "Output";

                    await ProcessDialogAndGenerateReport(outputType, outputPath, testPass, botConnector, dialogReports, connectionEstablished, dialogResults, fileContents, isFirstDialog, tests, testName, false, inputFileName, allInputFilesTestReport, outputFileName, appSettings).ConfigureAwait(false);
                }
            }
#if USE_ARIA_LOGGING
                    AriaLogger.Stop();
#endif
            return testPass;
        }

        private static async Task ProcessDialogAndGenerateReport(string outputType, string outputPath, bool testPass, BotConnector botConnector, List<DialogReport> dialogReports, bool connectionEstablished, List<DialogResult> dialogResults, List<Dialog> fileContents, bool isFirstDialog, TestSettings tests, string testName, bool sendFirst, string inputFileName, List<TestReport> allInputFilesTestReport, string outputFileName, AppSettings appSettings)
        {
            outputPath = Path.Combine(appSettings.OutputFolder, testName + outputType);
            testName = Path.GetFileNameWithoutExtension(outputPath);
            DirectoryInfo outputDirectory = Directory.CreateDirectory(outputPath);
            outputFileName = Path.Combine(outputDirectory.FullName, testName + ".json");
            testPass = await ProcessDialog(fileContents, botConnector, appSettings, isFirstDialog, tests, connectionEstablished, testName, dialogReports, testPass, dialogResults, sendFirst).ConfigureAwait(false);

            await ProcessTestReport(inputFileName, dialogReports, allInputFilesTestReport, botConnector, testPass, dialogResults, outputFileName, connectionEstablished, appSettings).ConfigureAwait(false);
        }

        private static async Task ProcessTestReport(string inputFileName, List<DialogReport> dialogReports, List<TestReport> allInputFilesTestReport, BotConnector botConnector, bool testPass, List<DialogResult> dialogResults, string outputFileName, bool connectionEstablished, AppSettings appSettings)
        {
            TestReport fileTestReport = new TestReport
            {
                FileName = inputFileName,
                DialogReports = dialogReports,
                DialogCount = dialogReports.Count,
            };
            fileTestReport.ComputeDialogPassRate();
            allInputFilesTestReport.Add(fileTestReport);

            File.WriteAllText(outputFileName, JsonConvert.SerializeObject(dialogResults, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            if (connectionEstablished)
            {
                await botConnector.Disconnect().ConfigureAwait(false);
                botConnector.Dispose();
            }

            File.WriteAllText(Path.Combine(appSettings.OutputFolder, ProgramConstants.TestReportFileName), JsonConvert.SerializeObject(allInputFilesTestReport, Formatting.Indented));

            Trace.IndentLevel = 0;
            if (testPass)
            {
                Trace.TraceInformation("********** TEST PASS **********");
            }
            else
            {
                Trace.TraceInformation("********** TEST FAILED **********");
            }
        }

        private static async Task<bool> ProcessDialog(
            List<Dialog> fileContents, BotConnector botConnector, AppSettings appSettings, bool isFirstDialog, TestSettings tests, bool connectionEstablished, string testName, List<DialogReport> dialogReports, bool testPass, List<DialogResult> dialogResults, bool sendFirst)
        {
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
                    Trace.TraceInformation($"[{DateTime.Now.ToString("hh:mm:ss.fff", CultureInfo.CurrentCulture)}] Running DialogId {dialog.DialogID}, description \"{dialog.Description}\"");
                }

                // Capture and compute the output for this dialog in this variable.
                DialogResult dialogResult = new DialogResult(appSettings, dialog.DialogID, dialog.Description);

                // Capture outputs of all turns in this dialog in this list.
                List<TurnResult> turnResults = new List<TurnResult>();

                // Keep track of turn pass/fail : per turn.
                List<bool> turnPassResults = new List<bool>();

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
                        // This sleep is to allow some time for the keyword recognizer to start up.
                        Thread.Sleep(100);
                    }

                    Trace.IndentLevel = 2;
                    Trace.TraceInformation($"[{DateTime.Now.ToString("hh:mm:ss.fff", CultureInfo.CurrentCulture)}] Running Turn {turn.TurnID}");
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

                    botConnector.SetInputValues(testName, dialog.DialogID, turn.TurnID, responseCount, tests.IgnoreActivities, turn.Keyword);

                    if (tests.WavAndUtterancePairs && !string.IsNullOrWhiteSpace(turn.WAVFile) && !string.IsNullOrWhiteSpace(turn.Utterance))
                    {
                        // Send up WAV File if present
                        if (!string.IsNullOrEmpty(turn.WAVFile) && sendFirst)
                        {
                            botConnector.SendAudio(turn.WAVFile);
                        }

                        // Send up Utterance if present
                        else if (!string.IsNullOrEmpty(turn.Utterance) && !sendFirst)
                        {
                            botConnector = await botConnector.Send(turn.Utterance).ConfigureAwait(false);
                        }
                    }

                    // WavAndUtterancePair is false send either wavfile or utterance if present.
                    else if (!tests.WavAndUtterancePairs)
                    {
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
                    }

                    // Send up activity if configured
                    else if (!string.IsNullOrEmpty(turn.Activity))
                    {
                        botConnector = await botConnector.SendActivity(turn.Activity).ConfigureAwait(false);
                    }

                    // All bot reply activities are captured in this variable.
                    dialogResult.BotResponses = botConnector.WaitAndProcessBotReplies(bootstrapMode);

                    // Capture the result of this turn in this variable and validate the turn.
                    TurnResult turnResult = dialogResult.BuildOutput(turn, bootstrapMode, botConnector.RecognizedText, botConnector.RecognizedKeyword);
                    if (!dialogResult.ValidateTurn(turnResult, bootstrapMode))
                    {
                        testPass = false;
                    }

                    // Add the turn result to the list of turn results.
                    turnResults.Add(turnResult);

                    // Add the turn completion status to the list of turn completions.
                    turnPassResults.Add(turnResult.Pass);

                    if (turn.Keyword)
                    {
                        await botConnector.StopKeywordRecognitionAsync().ConfigureAwait(false);
                    }
                } // End of turns loop

                dialogResult.Turns = turnResults;
                dialogResults.Add(dialogResult);

                DialogReport dialogReport = new DialogReport(dialogResult.DialogID, dialog.Description, turnPassResults);
                dialogReports.Add(dialogReport);
                turnPassResults = new List<bool>();

                Trace.IndentLevel = 1;
                if (dialogReport.DialogPass)
                {
                    Trace.TraceInformation($"DialogId {dialog.DialogID} passed");
#if USE_ARIA_LOGGING
                        AriaLogger.Log(AriaLogger.EventNameDialogSucceeded, dialog.DialogID, dialog.Description);
#endif
                }
                else
                {
                    Trace.TraceInformation($"DialogId {dialog.DialogID} failed");
#if USE_ARIA_LOGGING
                        AriaLogger.Log(AriaLogger.EventNameDialogFailed, dialog.DialogID, dialog.Description);
#endif
                }
            } // End of dialog loop

            // Always clean up our connector object
            if (botConnector != null)
            {
                await botConnector.Disconnect().ConfigureAwait(false);
                botConnector.Dispose();
            }

            return testPass;
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
                Stream logFileStream = File.Create(Path.Combine(outputFolder, ProgramConstants.TestLogFileName));
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
        private static (bool ValidActivity, string ErrorString) CheckValidActivity(string activity)
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
                if (!int.TryParse(latency[1], out int parsedIndex) || (parsedIndex >= expectedResponseSize) || parsedIndex < 0)
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
        /// Checks if the Expected TTS Duration has valid values.
        /// </summary>
        /// <param name="expectedTTSAudioDurations"> ExpectedTTSAudioDuration.</param>
        /// <returns>True if the ListExpected TTS Duration has valid values else false.</returns>
        private static bool CheckValidExpectedTTSAudioDuration(List<string> expectedTTSAudioDurations)
        {
            foreach (string expectedTTSAudioDuration in expectedTTSAudioDurations)
            {
                foreach (string strDuration in expectedTTSAudioDuration.Split(ProgramConstants.OROperator))
                {
                    int intDuration;
                    if (!int.TryParse(strDuration, out intDuration))
                    {
                        return false;
                    }

                    if (intDuration <= 0 && intDuration != -1)
                    {
                        return false;
                    }
                }
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
        private static (bool ValidTurn, List<string> ExceptionMesssage) ValidateTurnInput(Turn turn, bool botGreeting, bool singleConnection, bool firstDialog, bool firstTurn)
        {
            bool utterancePresentValid = CheckNotNullNotEmptyString(turn.Utterance);
            bool activityPresentValid = CheckNotNullNotEmptyString(turn.Activity);
            bool wavFilePresentValid = CheckNotNullNotEmptyString(turn.WAVFile);
            bool expectedLatencyPresentValid = CheckNotNullNotEmptyString(turn.ExpectedUserPerceivedLatency);

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
                    var expectedLatencyObjectValid = CheckValidExpectedLatency(turn.ExpectedUserPerceivedLatency, turn.ExpectedResponses.Count);
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

            if (turn.ExpectedResponses != null && turn.ExpectedResponses.Count != 0 && turn.ExpectedTTSAudioResponseDurations != null)
            {
                if (turn.ExpectedTTSAudioResponseDurations.Count != turn.ExpectedResponses.Count)
                {
                    exceptionMessage.Add(ErrorStrings.TTS_AUDIO_DURATION_INVALID);
                }
                else
                {
                    for (int i = 0; i < turn.ExpectedResponses.Count; i++)
                    {
                        Activity expectedResponse = turn.ExpectedResponses[i];
                        int orsInText = (expectedResponse.Text == null) ? 0 : expectedResponse.Text.Split(ProgramConstants.OROperator).Length - 1;
                        int orsInSpeak = (expectedResponse.Speak == null) ? 0 : expectedResponse.Speak.Split(ProgramConstants.OROperator).Length - 1;
                        int orsInExpectedTTSAudioResponseDuration = turn.ExpectedTTSAudioResponseDurations[i].Split(ProgramConstants.OROperator).Length - 1;

                        if (orsInText != orsInSpeak || orsInSpeak != orsInExpectedTTSAudioResponseDuration)
                        {
                            exceptionMessage.Add(ErrorStrings.OR_OCCURRENCE_INCONSISTENT);
                        }
                    }

                    if (!CheckValidExpectedTTSAudioDuration(turn.ExpectedTTSAudioResponseDurations))
                    {
                        exceptionMessage.Add(ErrorStrings.TTS_AUDIO_DURATION_VALUES_INVALID);
                    }
                }
            }

            if ((turn.ExpectedResponses == null || turn.ExpectedResponses.Count == 0) && turn.ExpectedTTSAudioResponseDurations != null)
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

            uniqueDialog.Sort();

            for (int i = 0; i < uniqueDialog.Count - 1; i++)
            {
                if (uniqueDialog[i] == uniqueDialog[i + 1])
                {
                    throw new ArgumentException($"{ErrorStrings.DUPLICATE_DIALOGID} - {uniqueDialog[i]}");
                }
            }
        }

        private static void ValidateTurnID(Turn turn, int turnIndex)
        {
            if (turn.TurnID < 0)
            {
                throw new ArgumentException($"{ErrorStrings.NEGATIVE_TURNID} - {turn.TurnID}");
            }

            if (turn.TurnID != turnIndex)
            {
                throw new ArgumentException($"{ErrorStrings.INVALID_TURNID_SEQUENCE} - {turn.TurnID}");
            }
        }
    }
}
