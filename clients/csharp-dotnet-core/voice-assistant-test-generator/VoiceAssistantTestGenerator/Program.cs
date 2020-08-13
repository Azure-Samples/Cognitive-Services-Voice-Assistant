// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTestGenerator
{
    /// <summary>
    /// Main Program class that contains the entry point.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            CommandLineArgs commandLineArgs = new CommandLineArgs(args);

            if (string.IsNullOrEmpty(commandLineArgs.TSVFile))
            {
                commandLineArgs.PrintUsage();
                return;
            }

            RunGeneration(commandLineArgs);
        }

        private static void RunGeneration(CommandLineArgs commandLineArgs)
        {
            if (commandLineArgs.TSVFile != null)
            {
                TestFileGenerator.GenerateTestFile(commandLineArgs.TSVFile, commandLineArgs.OutputFile);
            }
        }
    }
}
