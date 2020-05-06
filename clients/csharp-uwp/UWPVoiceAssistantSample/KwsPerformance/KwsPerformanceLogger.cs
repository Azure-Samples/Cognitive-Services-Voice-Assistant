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
        private static KeywordDetectionParams keywordDetectionParams = new KeywordDetectionParams();

        private string filePath = $"{ApplicationData.Current.LocalFolder.Path}\\kwsPerformanceMetrics.csv";

        private bool csvFileCreated;

        public static TimeSpan kwsEventFireTime;
        public static TimeSpan kwsStartTime;

        /// <summary>
        /// Sets the keyword stage, confirmation bool, and elapsed time for KWS and KWV.
        /// </summary>
        /// <param name="stage">Stage of KWS</param>
        /// <param name="confirmed">Bool indicating if speech matches keyword model.</param>
        /// <param name="elapsedTime">Timespan for keyword confirmation.</param>
        public void LogSignalReceived(string stage, bool confirmed, long eventFireTime, long startTime, long endTime)
        {
            if (!this.csvFileCreated)
            {
                this.Initialize().Wait();
            }

            keywordDetectionParams.Stage = stage;
            keywordDetectionParams.Confirmed = confirmed;
            keywordDetectionParams.EventFireTime = eventFireTime;
            keywordDetectionParams.StartTime = startTime;
            keywordDetectionParams.EndTime = endTime;

            this.WriteToCSV().Wait();
        }

        private async Task Initialize()
        {
            if (!File.Exists(this.filePath))
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Stage, Confirmed, EventFireTime, StartTime, EndTime");

                await File.AppendAllTextAsync(this.filePath, sb.ToString());

                this.csvFileCreated = true;
                return;
            }

            this.csvFileCreated = false;
        }

        private async Task WriteToCSV()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"\"{keywordDetectionParams.Stage}\",\"{keywordDetectionParams.Confirmed}\",\"{keywordDetectionParams.EventFireTime}\",\"{keywordDetectionParams.StartTime}\",\"{keywordDetectionParams.EndTime}\"");

            await File.AppendAllTextAsync(this.filePath, sb.ToString());
        }
    }
}
