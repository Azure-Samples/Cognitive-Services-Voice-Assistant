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

namespace InventoryApp
{
    public static class InventoryDemo
    {
        [FunctionName("InventoryDemo")]
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

            CloudTable table = tableClient.GetTableReference("InventoryData");

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
                var getRoom = TableOperation.Retrieve<InventoryData>(partitionKey, rowKey);

                var query = await table.ExecuteAsync(getRoom);

                var currInventoryData = (InventoryData)query.Result;

                // if room not exist, create a record using default data
                if (currInventoryData == null)
                {
                    var defaultRoom = new InventoryData(partitionKey, rowKey);
                    var createRoom = TableOperation.Insert(defaultRoom);
                    await table.ExecuteAsync(createRoom);
                    currInventoryData = (InventoryData)(await table.ExecuteAsync(getRoom)).Result;
                }

                var operation = req.Query["operation"].ToString().ToLower();
                var product = req.Query["product"].ToString().ToLower();
                int quantity = (!string.IsNullOrEmpty(req.Query["quantity"].ToString())) ? int.Parse(req.Query["quantity"].ToString()) : 0;
                var updated = false;

                log.LogInformation($"Executing {operation} on {(string.IsNullOrEmpty(product) ? "no product" : product)} for {quantity}");

                if (!string.IsNullOrEmpty(operation))
                {
                    if (operation.Equals("reset"))
                    {
                        currInventoryData.LoadDefaultData();
                        updated = true;
                    }
                    else if (operation.Equals("help"))
                    {
                        currInventoryData.Help = true;
                        updated = true;
                    }
                    else if (operation.Equals("query"))
                    {
                        //This is to ensure the json data object is returned to Custom Commands.
                        updated = true;
                    }
                    else if (operation.Equals("remove"))
                    {
                        currInventoryData.Help = false;
                        if (product.Equals("blue"))
                        {
                            if (currInventoryData.FirstItem < quantity)
                            {
                                currInventoryData.Message = $"Can not ship {quantity} blue boxes because there are only {currInventoryData.FirstItem} in stock.";
                            }
                            else
                            {
                                currInventoryData.FirstItem -= quantity;
                                currInventoryData.Message = $"Shipped {quantity} blue boxes";
                            }
                            updated = true;
                        }
                        else if (product.Equals("yellow"))
                        {
                            if (currInventoryData.SecondItem < quantity)
                            {
                                currInventoryData.Message = $"Can not ship {quantity} yellow boxes because there are only {currInventoryData.SecondItem} in stock.";
                            }
                            else
                            {
                                currInventoryData.SecondItem -= quantity;
                                currInventoryData.Message = $"Shipped {quantity} yellow boxes";
                            }
                            updated = true;
                        }
                        else if (product.Equals("green"))
                        {
                            if (currInventoryData.ThirdItem < quantity)
                            {
                                currInventoryData.Message = $"Can not ship {quantity} green boxes because there are only {currInventoryData.ThirdItem} in stock.";
                            }
                            else
                            {
                                currInventoryData.ThirdItem -= quantity;
                                currInventoryData.Message = $"Shipped {quantity} green boxes";
                            }
                            updated = true;
                        }
                        else
                        {
                            currInventoryData.Help = true;
                            updated = true;
                        }
                    }
                    else if (operation.Equals("deplete"))
                    {
                        currInventoryData.Help = false;
                        if (product.Equals("blue"))
                        {
                            currInventoryData.Message = $"Shipped all {currInventoryData.FirstItem} blue boxes";
                            currInventoryData.FirstItem = 0;

                            updated = true;
                        }
                        else if (product.Equals("yellow"))
                        {
                            currInventoryData.Message = $"Shipped all {currInventoryData.SecondItem} yellow boxes";
                            currInventoryData.SecondItem = 0;

                            updated = true;
                        }
                        else if (product.Equals("green"))
                        {
                            currInventoryData.Message = $"Shipped all {currInventoryData.ThirdItem} green boxes";
                            currInventoryData.ThirdItem = 0;

                            updated = true;
                        }
                        else
                        {
                            currInventoryData.FirstItem = 0;
                            currInventoryData.SecondItem = 0;
                            currInventoryData.ThirdItem = 0;
                            currInventoryData.Message = "Shipped all items";
                            updated = true;
                        }
                    }
                    else if (operation.Equals("add"))
                    {
                        currInventoryData.Help = false;
                        if (product.Equals("blue"))
                        {
                            currInventoryData.FirstItem += quantity;
                            currInventoryData.Message = $"Received {quantity} blue boxes";
                            updated = true;
                        }
                        else if (product.Equals("yellow"))
                        {
                            currInventoryData.SecondItem += quantity;
                            currInventoryData.Message = $"Received {quantity} yellow boxes";
                            updated = true;
                        }
                        else if (product.Equals("green"))
                        {
                            currInventoryData.ThirdItem += quantity;
                            currInventoryData.Message = $"Received {quantity} green boxes";
                            updated = true;
                        }
                        else
                        {
                            currInventoryData.Help = true;
                            updated = true;
                        }
                    }
                }

                if (updated)
                {
                    var updateRoom = TableOperation.Replace(currInventoryData as InventoryData);
                    await table.ExecuteAsync(updateRoom);
                    log.LogInformation("successfully updated the record");
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(currInventoryData, Formatting.Indented), Encoding.UTF8, "application/json")
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

    public class InventoryData : TableEntity
    {
        public InventoryData() { }
        public int FirstItem { get; set; }
        public int SecondItem { get; set; }
        public int ThirdItem { get; set; }
        public bool Help { get; set; }
        public string Message { get; set; }

        public InventoryData(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
            this.FirstItem = 11;
            this.SecondItem = 15;
            this.ThirdItem = 4;
            this.Help = true;
            this.Message = "";
        }

        public void LoadDefaultData()
        {
            this.FirstItem = 11;
            this.SecondItem = 15;
            this.ThirdItem = 4;
            this.Help = true;
            this.Message = "";
        }
    }
}