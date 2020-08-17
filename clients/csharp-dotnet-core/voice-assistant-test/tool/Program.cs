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
            return MainMethod(args.Length == 0 ? null : args[0]).GetAwaiter().GetResult();
        }

        private static async Task<int> MainMethod(string configFile)
        {
            try
            {
                bool testPass = await MainService.StartUp(configFile).ConfigureAwait(false);

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
    }
}