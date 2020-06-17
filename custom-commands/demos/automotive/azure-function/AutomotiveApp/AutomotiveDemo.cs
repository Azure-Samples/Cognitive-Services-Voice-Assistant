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

            CloudTable table = tableClient.GetTableReference("AutomotiveData");

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
                var getRoom = TableOperation.Retrieve<AutomotiveData>(partitionKey, rowKey);

                var query = await table.ExecuteAsync(getRoom);

                var currentAutomotiveData = (AutomotiveData)query.Result;

                // if room not exist, create a record using default data
                if (currentAutomotiveData == null)
                {
                    var defaultRoom = new AutomotiveData(partitionKey, rowKey);
                    var createRoom = TableOperation.Insert(defaultRoom);
                    await table.ExecuteAsync(createRoom);
                    currentAutomotiveData = (AutomotiveData)(await table.ExecuteAsync(getRoom)).Result;
                }

                string operation = req.Query["operation"].ToString().ToLower();
                string strValue = req.Query["value"].ToString().ToLower();
                int.TryParse(strValue, out int intValue);
                bool updated = false;

                if (!string.IsNullOrEmpty(operation))
                {
                    if (operation.Equals("reset"))
                    {
                        currentAutomotiveData.LoadDefaultData();
                        currentAutomotiveData.Message = "Okay, reset to default state.";
                        updated = true;
                    }
                    else if (operation.Equals("help"))
                    {
                        currentAutomotiveData.Help = true;
                        currentAutomotiveData.Message = "You're in a virtual car and able to control features with your voice. Try saying \"Turn on the seat warmers\" or \"Set the temperature to 73 degrees\"";
                        updated = true;
                    }
                    else
                    {
                        currentAutomotiveData.Help = false;

                        if (operation.Equals("settemperature"))
                        {
                            currentAutomotiveData.Temperature = intValue;
                            currentAutomotiveData.Message = $"Okay, set temperature to {intValue} degrees";
                            updated = true;
                        }
                        else if (operation.Equals("increasetemperature"))
                        {
                            currentAutomotiveData.Temperature += intValue;
                            currentAutomotiveData.Message = $"All right, raise the temperature by {intValue} degrees";
                            updated = true;
                        }
                        else if (operation.Equals("decreasetemperature"))
                        {
                            currentAutomotiveData.Temperature -= intValue;
                            currentAutomotiveData.Message = $"All right, lower the temperature by {intValue} degrees";
                            updated = true;
                        }
                        else if (operation.Equals("defrost") || operation.Equals("seatwarmer"))
                        {
                            bool? valueBool = (strValue.Equals("on")) ? true : ((strValue.Equals("off")) ? (bool?)false : null);

                            if (valueBool == null)
                            {
                                updated = false;
                            }
                            else if (operation.Equals("defrost"))
                            {
                                if (currentAutomotiveData.Defrost == (bool)valueBool)
                                {
                                    currentAutomotiveData.Message = $"Defrost already {strValue}";
                                }
                                else
                                {
                                    currentAutomotiveData.Defrost = (bool)valueBool;
                                    currentAutomotiveData.Message = $"Ok, turn defroster {strValue}";
                                }
                                updated = true;
                            }
                            else if (operation.Equals("seatwarmer"))
                            {
                                if (currentAutomotiveData.SeatWarmers == (bool)valueBool)
                                {
                                    currentAutomotiveData.Message = $"Seat warmer already {strValue}";
                                }
                                else
                                {
                                    currentAutomotiveData.SeatWarmers = (bool)valueBool;
                                    currentAutomotiveData.Message = $"Ok, turn seat warmer {strValue}";
                                }
                                updated = true;
                            }
                        }
                        else
                        {
                            currentAutomotiveData.Help = true;
                        }
                    }
                }

                if (updated)
                {
                    var updateRoom = TableOperation.Replace(currentAutomotiveData as AutomotiveData);
                    await table.ExecuteAsync(updateRoom);
                    log.LogInformation("successfully updated the record");
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(currentAutomotiveData, Formatting.Indented), Encoding.UTF8, "application/json")
                };
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Failed to process request")
                };
            }
        }
    }

    public class AutomotiveData : TableEntity
    {
        public AutomotiveData() { }
        public int Temperature { get; set; }
        public bool SeatWarmers { get; set; }
        public bool Defrost { get; set; }
        public bool Help { get; set; }
        public string Message { get; set; }

        public AutomotiveData(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
            this.Temperature = 68;
            this.SeatWarmers = false;
            this.Defrost = false;
            this.Help = true;
            this.Message = "";
        }

        public void LoadDefaultData()
        {
            this.Temperature = 68;
            this.SeatWarmers = false;
            this.Defrost = false;
            this.Help = true;
            this.Message = "";
        }
    }
}