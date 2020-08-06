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
