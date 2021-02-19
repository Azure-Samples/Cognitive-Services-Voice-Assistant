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

                if (string.IsNullOrEmpty(operation))
                {
                    // Return the current inventory state. Do not alter client's knowledge of previous state (clientContext).
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(currInventoryData, Formatting.Indented), Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    // Store state before change for Custom Commands clientContext
                    var clientContext = currInventoryData.GetDeepCopy();

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
                        // On query, return only the current inventory state. Do not alter client's knowledge of previous state (clientContext).
                        updated = true;
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(JsonConvert.SerializeObject(currInventoryData, Formatting.Indented), Encoding.UTF8, "application/json")
                        };
                    }
                    else if (operation.Equals("remove"))
                    {
                        currInventoryData.Help = false;
                        if (product.Equals("blue"))
                        {
                            if (quantity <= currInventoryData.BlueItemCount)
                            {
                                currInventoryData.BlueItemCount -= quantity;
                            }
                            updated = true;
                        }
                        else if (product.Equals("yellow"))
                        {
                            if (quantity <= currInventoryData.YellowItemCount)
                            {
                                currInventoryData.YellowItemCount -= quantity;
                            }
                            updated = true;
                        }
                        else if (product.Equals("green"))
                        {
                            if (quantity <= currInventoryData.GreenItemCount)
                            {
                                currInventoryData.GreenItemCount -= quantity;
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
                            //currInventoryData.Message = $"Shipped all {currInventoryData.BlueItemCount} blue boxes";
                            currInventoryData.BlueItemCount = 0;

                            updated = true;
                        }
                        else if (product.Equals("yellow"))
                        {
                            //currInventoryData.Message = $"Shipped all {currInventoryData.YellowItemCount} yellow boxes";
                            currInventoryData.YellowItemCount = 0;

                            updated = true;
                        }
                        else if (product.Equals("green"))
                        {
                            //currInventoryData.Message = $"Shipped all {currInventoryData.GreenItemCount} green boxes";
                            currInventoryData.GreenItemCount = 0;

                            updated = true;
                        }
                        else
                        {
                            currInventoryData.BlueItemCount = 0;
                            currInventoryData.YellowItemCount = 0;
                            currInventoryData.GreenItemCount = 0;
                            //currInventoryData.Message = "Shipped all items";
                            updated = true;
                        }
                    }
                    else if (operation.Equals("add"))
                    {
                        currInventoryData.Help = false;
                        if (product.Equals("blue"))
                        {
                            currInventoryData.BlueItemCount += quantity;
                            //currInventoryData.Message = $"Received {quantity} blue boxes";
                            updated = true;
                        }
                        else if (product.Equals("yellow"))
                        {
                            currInventoryData.YellowItemCount += quantity;
                            //currInventoryData.Message = $"Received {quantity} yellow boxes";
                            updated = true;
                        }
                        else if (product.Equals("green"))
                        {
                            currInventoryData.GreenItemCount += quantity;
                            //currInventoryData.Message = $"Received {quantity} green boxes";
                            updated = true;
                        }
                        else
                        {
                            currInventoryData.Help = true;
                            updated = true;
                        }
                    }

                    if (updated)
                    {
                        var updateRoom = TableOperation.Replace(currInventoryData as InventoryData);
                        await table.ExecuteAsync(updateRoom);
                        log.LogInformation("successfully updated the record");
                    }

                    var stateChangeData = new InventoryDataStateChange(clientContext, currInventoryData);

                    // Return results of the (attempted) operation with before and after states.
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(stateChangeData, Formatting.Indented), Encoding.UTF8, "application/json")
                    };
                }
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

    public class InventoryDataStateChange
    {
        public InventoryData clientContext;
        public InventoryData currentState;

        public InventoryDataStateChange(InventoryData clientContext, InventoryData currentState)
        {
            this.clientContext = clientContext;
            this.currentState = currentState;
        }
    }

    public class InventoryData : TableEntity
    {
        public InventoryData() { }
        public int BlueItemCount { get; set; }
        public int YellowItemCount { get; set; }
        public int GreenItemCount { get; set; }
        public bool Help { get; set; }
        public string Message { get; set; }

        public InventoryData(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
            this.BlueItemCount = 11;
            this.YellowItemCount = 15;
            this.GreenItemCount = 4;
            this.Help = true;
            this.Message = "";
        }

        public void LoadDefaultData()
        {
            this.BlueItemCount = 11;
            this.YellowItemCount = 15;
            this.GreenItemCount = 4;
            this.Help = true;
            this.Message = "";
        }

        public InventoryData GetDeepCopy()
        {
            var copy = new InventoryData(this.PartitionKey, this.RowKey);
            copy.BlueItemCount = this.BlueItemCount;
            copy.YellowItemCount = this.YellowItemCount;
            copy.GreenItemCount = this.GreenItemCount;
            copy.Help = this.Help;
            copy.Message = this.Message;

            return copy;
        }
    }
}