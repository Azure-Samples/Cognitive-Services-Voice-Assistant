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

        /// <summary>
        /// 
        /// </summary>
        public static TimeSpan kwsEventFireTime;
        
        /// <summary>
        /// 
        /// </summary>
        public static TimeSpan kwsStartTime;

        /// <summary>
        /// Sets the keyword stage, confirmation bool, and elapsed time for KWS and KWV.
        /// </summary>
        /// <param name="stage">Stage of KWS</param>
        /// <param name="confirmed">Bool indicating if speech matches keyword model.</param>
        /// <param name="eventFireTime">Value in ticks indicating time of event.</param>
        /// <param name="startTime">Value in ticks indicating start time of kws.</param>
        /// <param name="endTime">Value in ticks indicating end time of kws.</param>
        public void LogSignalReceived(string stage, string confirmed, long eventFireTime, long startTime, long endTime)
        {
            if (!this.csvFileCreated)
            {
                this.Initialize();
            }

            keywordDetectionParams.Stage = stage;
            keywordDetectionParams.Confirmed = confirmed;
            keywordDetectionParams.EventFireTime = eventFireTime;
            keywordDetectionParams.StartTime = startTime;
            keywordDetectionParams.EndTime = endTime;

            this.WriteToCSV().Wait();
        }

        private void Initialize()
        {
            if (!File.Exists(this.filePath))
            {
                // StringBuilder sb = new StringBuilder();

                // sb.AppendLine("Stage, Confirmed, EventFireTime, StartTime, EndTime");

                // await File.AppendAllTextAsync(this.filePath, sb.ToString());

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
