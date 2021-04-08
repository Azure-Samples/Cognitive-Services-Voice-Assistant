using Microsoft.WindowsAzure.Storage.Table;

namespace DeviceState
{
    public class DeviceConfig : TableEntity
    {
        public DeviceConfig() { }
        public string TV { get; set; }
        public string Fan { get; set; }

        public DeviceConfig(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            TV = "off";
            Fan = "off";
        }

        public void LoadDefaultConfig()
        {
            TV = "off";
            Fan = "off";
        }
    }
}
