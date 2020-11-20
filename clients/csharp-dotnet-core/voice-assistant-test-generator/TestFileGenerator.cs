// <copyright file="TestFileGenerator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace VoiceAssistantTestGenerator
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class that handles generating the json test files for the VoiceAssistantTest program.
    /// </summary>
    public static class TestFileGenerator
    {
        private const string TABSPACES = "    ";
        private const string HEADERWAVFILE = "WavFile";
        private const string HEADERUTTERANCE = "Utterance";
        private const string HEADERKEYWORD = "Keyword";
        private const string HEADERSLEEP = "Sleep";
        private const string HEADEREXPECTEDRESPONSES = "ExpectedResponses";

        /// <summary>
        /// Calls all functions necessary to generate the test.json file.
        /// </summary>
        /// <param name="tsvFilePath"> the path to the tab separated input file.</param>
        /// /// <param name="outputFilePath"> the path to the output test.json file.</param>
        public static void GenerateTestFile(string tsvFilePath, string outputFilePath)
        {
            CheckFilePath("tsv file", tsvFilePath);
            GenerateTests(tsvFilePath, outputFilePath);
        }

        private static void GenerateTests(string tsvFilePath, string outputFilePath)
        {
            var tsvFile = File.OpenRead(tsvFilePath);
            var outputFile = File.Open(outputFilePath, FileMode.Create);

            StreamReader streamReader = new StreamReader(tsvFile);
            StreamWriter streamWriter = new StreamWriter(outputFile);

            var columnHeaders = ReadColumnHeaders(streamReader);

            // begin the list of dialogs in the test file.
            streamWriter.WriteLine("[");

            int testCount = 1;

            // this starts at 1 because of the first [
            int indentation = 1;
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();

                // write the comma if this isn't the first dialog
                if (testCount != 1)
                {
                    streamWriter.WriteLine(",");
                }
                else
                {
                    streamWriter.WriteLine();
                }

                string[] columns = line.Split('\t');

                // write beginning of dialog
                WriteIndentationLine(streamWriter, indentation, "{");
                indentation++;
                WriteIndentationLine(streamWriter, indentation, "\"DialogID\": " + testCount + ",");

                int columnIndex = 0;
                bool knownColumnHeaders = true;
                var currentDialog = new DialogData();
                while (knownColumnHeaders)
                {
                    switch (columnHeaders[columnIndex])
                    {
                        case HEADERUTTERANCE:
                            currentDialog.Utterance = columns[columnIndex];
                            break;
                        case HEADERWAVFILE:
                            // If WavFile is specified, we use the WAV file input
                            bool isWindows = System.Runtime.InteropServices.RuntimeInformation
                                                       .IsOSPlatform(OSPlatform.Windows);
                            var wavFilePath = columns[columnIndex];
                            if (isWindows)
                            {
                                currentDialog.WavFile = wavFilePath.Replace("\\", "\\\\", System.StringComparison.Ordinal);
                            }

                            break;
                        case HEADERKEYWORD:
                            currentDialog.Keyword = bool.Parse(columns[columnIndex]);
                            break;
                        case HEADERSLEEP:
#pragma warning disable CA1305 // Specify IFormatProvider
                            currentDialog.Sleep = int.Parse(columns[columnIndex]);
#pragma warning restore CA1305 // Specify IFormatProvider
                            break;
                        case HEADEREXPECTEDRESPONSES:
                        default:
                            knownColumnHeaders = false;
                            continue;
                    }

                    columnIndex++;
                }

                WriteIndentationLine(streamWriter, indentation, "\"Description\": \"Dialog - " + testCount + "\",");
                WriteIndentationLine(streamWriter, indentation, "\"Skip\": false,");
                WriteIndentationLine(streamWriter, indentation, "\"Turns\": [");
                indentation++;
                WriteIndentationLine(streamWriter, indentation, "{");
                indentation++;
                WriteIndentationLine(streamWriter, indentation, "\"TurnID\": 0,");
                WriteIndentationLine(streamWriter, indentation, $"\"Sleep\": {currentDialog.Sleep},");
                WriteIndentationLine(streamWriter, indentation, "\"Activity\": \"\",");
                WriteIndentationLine(streamWriter, indentation, $"\"Keyword\": {currentDialog.Keyword.ToString().ToLower(CultureInfo.CurrentCulture)},");
                WriteIndentationLine(streamWriter, indentation, "\"Utterance\": \"" + currentDialog.Utterance + "\",");
                WriteIndentationLine(streamWriter, indentation, "\"WavFile\": \"" + currentDialog.WavFile + "\",");

                WriteIndentationLine(streamWriter, indentation, "\"ExpectedResponses\": [");
                indentation++;
                WriteIndentationLine(streamWriter, indentation, "{");
                indentation++;

                if (columns.Length > columnHeaders.Length)
                {
                    Console.WriteLine("ERROR: line found with more columns than headers. Ignoring columns without headers.");
                }

                bool needComma = false;
                for (int i = columnIndex; i < columnHeaders.Length; i++)
                {
                    string header = columnHeaders[i];

                    if (header == HEADEREXPECTEDRESPONSES)
                    {
                        continue;
                    }

                    // handle commas between objects
                    if (needComma)
                    {
                        streamWriter.WriteLine(",");
                    }
                    else
                    {
                        needComma = true;
                    }

                    if (header.Trim().EndsWith('}'))
                    {
                        WriteIndents(streamWriter, indentation);
                        streamWriter.WriteLine("\"" + header.Trim().Trim('}') + "\": \"" + columns[i] + "\"");
                        indentation--;
                        WriteIndents(streamWriter, indentation);
                        streamWriter.WriteLine("}");
                        continue;
                    }

                    if (header.Trim().EndsWith('{'))
                    {
                        WriteIndents(streamWriter, indentation);
                        streamWriter.WriteLine("\"" + header.Trim('{') + "\": {");
                        indentation++;
                        needComma = false;
                        continue;
                    }

                    WriteIndents(streamWriter, indentation);
                    streamWriter.Write("\"" + header + "\": ");
                    streamWriter.Write("\"" + columns[i] + "\"");

                    // TODO add writing of non string json types.
                }

                indentation--;
                WriteIndentationLine(streamWriter, indentation, "}");
                indentation--;
                WriteIndentationLine(streamWriter, indentation, "]");
                indentation--;
                WriteIndentationLine(streamWriter, indentation, "}");
                indentation--;
                WriteIndentationLine(streamWriter, indentation, "]");
                indentation--;
                WriteIndents(streamWriter, indentation);
                streamWriter.Write("}");

                testCount++;
            }

            streamWriter.WriteLine();

            // end the list of dialogs in the test file.
            streamWriter.WriteLine("]");

            streamReader.Dispose();
            streamWriter.Dispose();
            outputFile.Dispose();
            tsvFile.Dispose();
        }

        private static string[] ReadColumnHeaders(StreamReader streamReader)
        {
            string[] columnHeaders;

            string line = streamReader.ReadLine();

            columnHeaders = line.Split('\t');

            return columnHeaders;
        }

        private static void CheckFilePath(string fileType, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new Exception("ERROR: " + fileType + " File Path is null or empty.");
            }

            if (!File.Exists(filePath))
            {
                throw new Exception("ERROR: " + fileType + "File does not exist. Please specify a valid path\nFilePath: " + filePath);
            }
        }

        private static void WriteIndentationLine(StreamWriter streamWriter, int tabs, string text)
        {
            WriteIndents(streamWriter, tabs);

            streamWriter.WriteLine(text);
        }

        private static void WriteIndents(StreamWriter streamWriter, int tabs)
        {
            for (int i = 0; i < tabs; i++)
            {
                streamWriter.Write(TABSPACES);
            }
        }
    }
}
