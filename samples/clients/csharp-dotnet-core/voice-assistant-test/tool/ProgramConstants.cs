// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    /// <summary>
    /// Class defining commonly used constants.
    /// </summary>
    internal class ProgramConstants
    {
        /// <summary>
        /// Name of the final report generated at the end of a test run.
        /// </summary>
        public const string TestReportFileName = "VoiceAssistantTestReport.json";

        /// <summary>
        /// Default TTS Duration margin to use (in milliseconds).
        /// </summary>
        public const int DefaultTTSAudioDurationMargin = 200;

        /// <summary>
        /// Default timeout to use (in milliseconds) while waiting for bot reply activities.
        /// </summary>
        public const int DefaultTimeout = 5000;

        /// <summary>
        /// Name of the sub folder to use under the main output test folder to write TTS responses.
        /// If this string is set to empty, then the TTS response WAV Files are written directly to the test Output folder.
        /// The test output folder is the OutputFolder/{testfile-name}Output/.
        /// </summary>
        public const string WAVFileFolderName = "WAVFiles";

        /// <summary>
        /// Configuration File Environment Variable for Test Method.
        /// </summary>
        public const string ConfigFileEnvVariable = "VOICE_ASSISTANT_TEST_CONFIG";

        /// <summary>
        /// Name for Default Configuration File.
        /// If ConfigFileEnvVariable is not defined, DefaultConfigFile will be used.
        /// </summary>
        public const string DefaultConfigFile = "VoiceAssistantTestConfig.json";
    }
}
