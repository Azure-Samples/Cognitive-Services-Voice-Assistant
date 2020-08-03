using System;

namespace VoiceAssistantTestGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLineArgs commandLineArgs = new CommandLineArgs(args);
        }

        private static void RunGeneration(CommandLineArgs commandLineArgs)
        {
            if (commandLineArgs.tsvFile != null)
            {
                TestFileGenerator.GenerateTestFile(commandLineArgs.tsvFile, commandLineArgs.outputFile);
            }
        }
    }
}
