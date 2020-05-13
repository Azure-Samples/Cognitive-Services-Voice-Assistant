using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Net.Http;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using System.IO;

namespace AutomotiveApp
{
    public static class AutomotiveDemo
    {
        [FunctionName("AutomotiveDemo")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string connectionsJson = File.ReadAllText(Path.Combine(context.FunctionAppDirectory, "Connections.json"));

            JObject ConnectionsObject = JObject.Parse(connectionsJson);

            string connectionString = ConnectionsObject["AZURE_STORAGE_URL"].ToString();

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("virtualroomconfig");


            await table.CreateIfNotExistsAsync();

            var room = req.Headers["room"];

            if (string.IsNullOrEmpty(room))
            {
                room = req.Query["room"];
            }

            if (string.IsNullOrEmpty(room))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Please pass a room name on the query string or in the header")
                };
            }

            var partitionKey = "Demo";
            var rowKey = room;

            try
            {
                // get the room from the table
                var getRoom = TableOperation.Retrieve<VirtualAutomotiveConfig>(partitionKey, rowKey);

                var query = await table.ExecuteAsync(getRoom);

                var currentAutomotiveConfig = (VirtualAutomotiveConfig)query.Result;

                // if room not exist, create a record using default config
                if (currentAutomotiveConfig == null)
                {
                    var defaultRoom = new VirtualAutomotiveConfig(partitionKey, rowKey);
                    var createRoom = TableOperation.Insert(defaultRoom);
                    await table.ExecuteAsync(createRoom);
                    currentAutomotiveConfig = (VirtualAutomotiveConfig)(await table.ExecuteAsync(getRoom)).Result;
                }

                var operation = req.Query["operation"].ToString().ToLower();
                var updated = false;

                if (!string.IsNullOrEmpty(operation))
                {
                    if (operation.Equals("reset"))
                    {
                        currentAutomotiveConfig.LoadDefaultConfig();
                        updated = true;
                    }
                    else if (operation.Equals("settemperature"))
                    {
                        currentAutomotiveConfig.Temperature = int.Parse(req.Query["value"]);
                        currentAutomotiveConfig.Message = "set temperature to " + req.Query["value"];
                        updated = true;
                    }
                    else if (operation.Equals("increasetemperature"))
                    {
                        currentAutomotiveConfig.Temperature += int.Parse(req.Query["value"]);
                        currentAutomotiveConfig.Message = "raised temperature by " + req.Query["value"] + " degrees";
                        updated = true;
                    }
                    else if (operation.Equals("decreasetemperature"))
                    {
                        currentAutomotiveConfig.Temperature -= int.Parse(req.Query["value"]);
                        currentAutomotiveConfig.Message = "decreased temperature by " + req.Query["value"] + " degrees";
                        updated = true;
                    }
                    else if (operation.Equals("defrost") || operation.Equals("seatwarmer"))
                    {
                        var value = req.Query["value"].ToString().ToLower();
                        bool? valueBool = (value.Equals("on")) ? true : ((value.Equals("off")) ? (bool?)false : null);

                        if (valueBool == null)
                        {
                            updated = false;
                        }
                        else if (operation.Equals("defrost"))
                        {
                            currentAutomotiveConfig.Defrost = (bool)valueBool;
                            currentAutomotiveConfig.Message = "Defrost " + value;
                            updated = true;
                        }
                        else if (operation.Equals("seatwarmer"))
                        {
                            currentAutomotiveConfig.SeatWarmers = (bool)valueBool;
                            currentAutomotiveConfig.Message = "Seat warmer " + value;
                            updated = true;
                        }
                    }
                }

                if (updated)
                {
                    var updateRoom = TableOperation.Replace(currentAutomotiveConfig as VirtualAutomotiveConfig);
                    await table.ExecuteAsync(updateRoom);
                    log.LogInformation("successfully updated the record");
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(currentAutomotiveConfig, Formatting.Indented), Encoding.UTF8, "application/json")
                };
            }
            catch(Exception e)
            {
                log.LogError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Failed to process request")
                };
            }
        }
    }

    public class VirtualAutomotiveConfig : TableEntity
    {
        public VirtualAutomotiveConfig() { }
        public int Temperature { get; set; }
        public bool SeatWarmers { get; set; }
        public bool Defrost { get; set; }
        public bool Help { get; set; }
        public string Message { get; set; }

        public VirtualAutomotiveConfig(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
            this.Temperature = 0;
            this.SeatWarmers = false;
            this.Defrost = false;
            this.Help = true;
            this.Message = "";
        }

        public void LoadDefaultConfig()
        {
            this.Temperature = 0;
            this.SeatWarmers = false;
            this.Defrost = false;
            this.Help = true;
            this.Message = "";
        }
    }
}