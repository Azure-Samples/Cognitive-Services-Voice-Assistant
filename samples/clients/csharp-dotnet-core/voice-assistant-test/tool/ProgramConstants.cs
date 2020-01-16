﻿// <copyright file="ProgramConstants.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
        public const string TestReportFileName = "VoiceAssistantTestReport.txt";

        /// <summary>
        /// Default TTS Duration margin to use (in milliseconds).
        /// </summary>
        public const string DefaultTTSAudioDurationMargin = "200";

        /// <summary>
        /// Default timeout to use (in milliseconds) while waiting for bot reply activities.
        /// </summary>
        public const string DefaultTimeout = "5000";

        /// <summary>
        /// Name of the sub folder to use under the main output test folder to write TTS responses.
        /// If this string is set to empty, then the TTS response WAV Files are written directly to the test Output folder.
        /// The test output folder is the OutputFolder/{testfile-name}Output/.
        /// </summary>
        public const string WAVFileFolderName = "WAVFiles";
    }
}