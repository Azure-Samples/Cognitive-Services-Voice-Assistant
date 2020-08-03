using System;

namespace VoiceAssistantTestGenerator
{
    class CommandLineArgs
    {
        public string configFile;
        public string tsvFile;
        public string outputFile = "DefaultOutput.json";
        public string[] args;

        private string lastFlag = string.Empty;

        public CommandLineArgs(string[] args)
        {
            this.Parse(args);
        }

        public void Parse(string[] args)
        {
            this.args = args;
            this.Parse();
        }

        private void Parse()
        {
            foreach (string arg in this.args)
            {
                if (!string.IsNullOrEmpty(lastFlag))
                {
                    this.AssignFlaggedArgument(lastFlag, arg);
                    lastFlag = string.Empty;
                }

                if (arg.StartsWith('-'))
                {
                    lastFlag = arg;
                    continue;
                }
                else
                {
                    this.configFile = arg;
                }
            }
        }

        private void AssignFlaggedArgument(string flag, string arg)
        {
            switch (flag)
            {
                case "-inputFile":
                case "-i":
                    this.tsvFile = arg;
                    break;
                case "-outputFile":
                case "-o":
                    this.outputFile = arg;
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown Flag passed as an argument. Flag: " + flag + " Arguement: " + arg);
                    throw new Exception("Unknown flag passed as an argument. Flag: " + flag + " Arguement: " + arg);
            }
        }


    }
}
