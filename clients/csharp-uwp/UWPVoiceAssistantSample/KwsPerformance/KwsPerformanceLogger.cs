namespace UWPVoiceAssistantSample.KwsPerformance
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Storage;

    public class KwsPerformanceLogger
    {
        public static KeywordDetectionParams keywordDetectionParams = new KeywordDetectionParams();

        private ILogProvider logger;

        private string filePath = $"{ApplicationData.Current.LocalFolder.Path}\\kwsPerformanceMetrics.csv";

        private bool performanceFileCreated;

        public KwsPerformanceLogger()
        {
            this.logger = LogRouter.GetClassLogger();
        }

        public async Task Initialize()
        {
            if (!File.Exists(this.filePath))
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Stage, Confirmed, Elapsed Time");

                await File.AppendAllTextAsync(this.filePath, sb.ToString());

                this.performanceFileCreated = true;
                return;
            }

            this.performanceFileCreated = false;
        }

        public void LogSignalReceived(string stage, bool confirmed, long elapsedTime)
        {
            if (!this.performanceFileCreated)
            {
                this.Initialize().Wait();
            }

            this.logger.Log(LogMessageLevel.Error, $"{stage}, {confirmed}, {elapsedTime}");

            keywordDetectionParams.Stage = stage;
            keywordDetectionParams.Confirmed = confirmed;
            keywordDetectionParams.KW_ElapsedTime = elapsedTime;

            this.WriteToCSV().Wait();
        }

        public async Task WriteToCSV()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"\"{keywordDetectionParams.Stage}\",\"{keywordDetectionParams.Confirmed}\",\"{keywordDetectionParams.KW_ElapsedTime}\"");

            await File.AppendAllTextAsync(this.filePath, sb.ToString());
        }
    }
}
