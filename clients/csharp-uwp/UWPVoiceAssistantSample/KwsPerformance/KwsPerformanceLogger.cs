using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace UWPVoiceAssistantSample.KwsPerformance
{
    public class KwsPerformanceLogger
    {
        public static KeywordDetectionParams keywordDetectionParams = new KeywordDetectionParams();

        private ILogProvider logger;

        private string filePath = $"{ApplicationData.Current.LocalFolder.Path}\\kwsPerformanceMetrics.csv";

        public KwsPerformanceLogger()
        {
            this.logger = LogRouter.GetClassLogger();
        }

        public void Initialize()
        {
            if (!File.Exists(this.filePath))
            {
                _ = File.Create(this.filePath);
            }
        }

        public void LogSignalReceived(string stage, bool confirmed, long elapsedTime)
        {
            Initialize();

            this.logger.Log("signal received kwsperformancelogger");

            keywordDetectionParams.Stage = stage;
            keywordDetectionParams.Confirmed = confirmed;
            keywordDetectionParams.KW_ElapsedTime = elapsedTime;

            WriteToCSV();
        }

        public void WriteToCSV()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"\"{keywordDetectionParams.Stage}\",\"{keywordDetectionParams.Confirmed}\",\"{keywordDetectionParams.KW_ElapsedTime}\"");

            File.AppendAllText(this.filePath, sb.ToString());
        }
    }
}
