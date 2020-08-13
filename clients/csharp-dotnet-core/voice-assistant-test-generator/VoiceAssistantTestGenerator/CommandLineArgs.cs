// <copyright file="CommandLineArgs.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace VoiceAssistantTestGenerator
{
    using System;

    /// <summary>
    /// Class that handles parsing the command line args and stores the values for use.
    /// </summary>
    public class CommandLineArgs
    {
        private string tsvFile;
        private string outputFile = "DefaultOutput.json";
        private string[] args;

        private string lastFlag = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgs"/> class.
        /// </summary>
        /// <param name="args"> the command line args.</param>
        public CommandLineArgs(string[] args)
        {
            this.Parse(args);
        }

        /// <summary>
        /// Gets or sets the tab separated filepath to be used as input for the test generator.
        /// </summary>
        public string TSVFile { get => this.tsvFile; set => this.tsvFile = value; }

        /// <summary>
        /// Gets or sets the filepath to output the generated tests.
        /// </summary>
        public string OutputFile { get => this.outputFile; set => this.outputFile = value; }

        /// <summary>
        /// Public method to that parses the command line args.
        /// </summary>
        /// <param name="args"> the command line args.</param>
        public void Parse(string[] args)
        {
            this.args = args;
            this.Parse();
        }

        /// <summary>
        /// Prints out the usage statement.
        /// </summary>
        public void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine(this.args[0] + " -i inputTabSeparatedFile [-o outputFile]");
        }

        private void Parse()
        {
            foreach (string arg in this.args)
            {
                if (!string.IsNullOrEmpty(this.lastFlag))
                {
                    this.AssignFlaggedArgument(this.lastFlag, arg);
                    this.lastFlag = string.Empty;
                }

                if (arg.StartsWith('-'))
                {
                    this.lastFlag = arg;
                    continue;
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
