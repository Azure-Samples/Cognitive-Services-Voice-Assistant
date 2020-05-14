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

namespace VirtualInventoryApp
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
                var getRoom = TableOperation.Retrieve<VirtualInventoryConfig>(partitionKey, rowKey);

                var query = await table.ExecuteAsync(getRoom);

                var currInventoryConfig = (VirtualInventoryConfig)query.Result;

                // if room not exist, create a record using default config
                if (currInventoryConfig == null)
                {
                    var defaultRoom = new VirtualInventoryConfig(partitionKey, rowKey);
                    var createRoom = TableOperation.Insert(defaultRoom);
                    await table.ExecuteAsync(createRoom);
                    currInventoryConfig = (VirtualInventoryConfig)(await table.ExecuteAsync(getRoom)).Result;
                }

                var operation = req.Query["operation"].ToString().ToLower();
                var product = req.Query["product"].ToString().ToLower();
                var quantity = req.Query["quantity"].ToString().ToLower();
                var updated = false;

                log.LogInformation($"Executing {operation} on {(string.IsNullOrEmpty(product) ? "no product" : product)} for {(string.IsNullOrEmpty(quantity) ? "0" : quantity)}");

                if (!string.IsNullOrEmpty(operation))
                {
                    if (operation.Equals("reset"))
                    {
                        currInventoryConfig.LoadDefaultConfig();
                        updated = true;
                    }
                    else if (operation.Equals("help"))
                    {
                        currInventoryConfig.Help = true;
                        updated = true;
                    }
                    else if (operation.Equals("query"))
                    {
                        //This is to ensure the json config object is returned to Custom Commands.
                        updated = true;
                    }
                    else if (operation.Equals("remove"))
                    {
                        currInventoryConfig.Help = false;
                        if (product.Equals("blue"))
                        {
                            currInventoryConfig.FirstItem -= int.Parse(quantity);
                            currInventoryConfig.Message = "Shipped " + quantity + " small items";
                            updated = true;
                        }
                        else if (product.Equals("yellow"))
                        {
                            currInventoryConfig.SecondItem -= int.Parse(quantity);
                            currInventoryConfig.Message = "Shipped " + quantity + " medium items";
                            updated = true;                        
                        }
                        else if (product.Equals("green"))
                        {
                            currInventoryConfig.ThirdItem -= int.Parse(quantity);
                            currInventoryConfig.Message = "Shipped " + quantity + " large items";
                            updated = true;
                        }
                        else
                        {
                            currInventoryConfig.Help = true;
                            updated = true;
                        }
                    }
                    else if (operation.Equals("deplete"))
                    {
                        currInventoryConfig.Help = false;
                        if (product.Equals("blue"))
                        {
                            currInventoryConfig.Message = "Shipped all " + currInventoryConfig.FirstItem + " small items";
                            currInventoryConfig.FirstItem = 0;
                            
                            updated = true;
                        }
                        else if (product.Equals("yellow"))
                        {
                            currInventoryConfig.Message = "Shipped all " + currInventoryConfig.SecondItem + " medium items";
                            currInventoryConfig.SecondItem = 0;
                            
                            updated = true;
                        }
                        else if (product.Equals("green"))
                        {
                            currInventoryConfig.Message = "Shipped all " + currInventoryConfig.ThirdItem + " large items";
                            currInventoryConfig.ThirdItem = 0;
                            
                            updated = true;
                        }
                        else
                        {
                            currInventoryConfig.FirstItem = 0;
                            currInventoryConfig.SecondItem = 0;
                            currInventoryConfig.ThirdItem = 0;
                            currInventoryConfig.Message = "Shipped all items";
                            updated = true;
                        }
                    }
                    else if (operation.Equals("add"))
                    {
                        currInventoryConfig.Help = false;
                        if (product.Equals("blue"))
                        {
                            currInventoryConfig.FirstItem += int.Parse(quantity);
                            currInventoryConfig.Message = "Received " + quantity + " small items";
                            updated = true;
                        }
                        else if (product.Equals("yellow"))
                        {
                            currInventoryConfig.SecondItem += int.Parse(quantity);
                            currInventoryConfig.Message = "Received " + quantity + " medium items";
                            updated = true;                        
                        }
                        else if (product.Equals("green"))
                        {
                            currInventoryConfig.ThirdItem += int.Parse(quantity);
                            currInventoryConfig.Message = "Received " + quantity + " large items";
                            updated = true;
                        }
                        else
                        {
                            currInventoryConfig.Help = true;
                            updated = true;
                        }
                    }
                }

                if (updated)
                {
                    var updateRoom = TableOperation.Replace(currInventoryConfig as VirtualInventoryConfig);
                    await table.ExecuteAsync(updateRoom);
                    log.LogInformation("successfully updated the record");
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(currInventoryConfig, Formatting.Indented), Encoding.UTF8, "application/json")
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

    public class VirtualInventoryConfig : TableEntity
    {
        public VirtualInventoryConfig() { }
        public int FirstItem { get; set; }
        public int SecondItem { get; set; }
        public int ThirdItem { get; set; }
        public bool Help { get; set; }
        public string Message { get; set; }

        public VirtualInventoryConfig(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
            this.FirstItem = 11;
            this.SecondItem = 15;
            this.ThirdItem = 4;
            this.Help = true;
            this.Message = "";
        }

        public void LoadDefaultConfig()
        {
            this.FirstItem = 11;
            this.SecondItem = 15;
            this.ThirdItem = 4;
            this.Help = true;
            this.Message = "";
        }
    }
}