// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace UWPVoiceAssistantSample.KwsPerformance
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Storage;

    /// <summary>
    /// Generates a CSV file indicating keyword spotting and verification for each stage.
    /// </summary>
    public class KwsPerformanceLogger
    {
        /// <summary>
        /// Timestamp for kws event initiation.
        /// </summary>
        public static TimeSpan KwsEventFireTime;

        /// <summary>
        /// Timestamp for keyword verification.
        /// </summary>
        public static TimeSpan KwsStartTime;

        /// <summary>
        /// String value indicating if 1st stage kws is hardware or software.
        /// </summary>
        public static string Spotter;

        private static KeywordDetectionParams keywordDetectionParams = new KeywordDetectionParams();

        private string filePath = $"{ApplicationData.Current.LocalFolder.Path}\\kwsPerformanceMetrics.csv";

        private bool csvFileCreated;

        /// <summary>
        /// Sets the keyword stage, confirmation bool, and elapsed time for KWS and KWV.
        /// </summary>
        /// <param name="spotter">Keyword recognition model is HWKWS or SWKWS.</param>
        /// <param name="confirmed">String value indicating if speech matches keyword model. "A" for accepted and "R" for rejected.</param>
        /// <param name="stage">Stage of KWS.</param>
        /// <param name="eventFireTime">Value in ticks indicating time of event.</param>
        /// <param name="startTime">Value in ticks indicating start time of kws.</param>
        /// <param name="endTime">Value in ticks indicating end time of kws.</param>
        public void LogSignalReceived(string spotter, string confirmed, string stage, long eventFireTime, long startTime, long endTime)
        {
            if (!this.csvFileCreated)
            {
                this.Initialize();
            }

            keywordDetectionParams.Spotter = spotter;
            keywordDetectionParams.Confirmed = confirmed;
            keywordDetectionParams.Stage = stage;
            keywordDetectionParams.EventFireTime = eventFireTime;
            keywordDetectionParams.StartTime = startTime;
            keywordDetectionParams.EndTime = endTime;

            if (LocalSettingsHelper.SetPropertyId != null)
            {
                this.WriteToCSV().Wait();
            }
        }

        private void Initialize()
        {
            if (!File.Exists(this.filePath))
            {
                // StringBuilder sb = new StringBuilder();
                // sb.AppendLine("Spotter, Confirmed, Stage, EventFireTime, StartTime, EndTime");
                // await File.AppendAllTextAsync(this.filePath, sb.ToString());

                this.csvFileCreated = true;
                return;
            }

            this.csvFileCreated = false;
        }

        private async Task WriteToCSV()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"\"{keywordDetectionParams.Spotter}\",\"{keywordDetectionParams.Confirmed}\",\"{keywordDetectionParams.Stage}\",\"{keywordDetectionParams.EventFireTime}\",\"{keywordDetectionParams.StartTime}\",\"{keywordDetectionParams.EndTime}\"");

            await File.AppendAllTextAsync(this.filePath, sb.ToString());
        }
    }
}
