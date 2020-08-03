// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantTest
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsulates the application's entry point.
    /// </summary>
    internal class Program
    {
        private static int Main(string[] args)
        {
            CommandLineArgs commandLineArgs = new CommandLineArgs(args);

            if (commandLineArgs.generationMode)
            {
                RunGeneration(commandLineArgs);
                return 0;
            }
            else
            {

                return RunTests(commandLineArgs).GetAwaiter().GetResult();
            }
        }

        private static async Task<int> RunTests(CommandLineArgs commandLineArgs)
        {
            try
            {
                bool testPass = await MainService.StartUp(commandLineArgs.configFile).ConfigureAwait(false);

                if (testPass)
                {
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine("Test failed");
                    return 1;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Test encountered exception");
                System.Diagnostics.Trace.TraceError(e.ToString());
                return 1;
            }
        }

        private static void RunGeneration(CommandLineArgs commandLineArgs)
        {
            if(commandLineArgs.tsvFile != null)
            {
                TestFileGenerator.GenerateTestFile(commandLineArgs.tsvFile, commandLineArgs.outputFile);
            }
        }
    }
}
