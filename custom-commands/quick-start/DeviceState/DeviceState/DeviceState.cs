using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace DeviceState
{
    public static class DeviceState
    {
        private static string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static string rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));

        private static string connectionsContent = File.ReadAllText(rootDirectory + "/Connections.json");
        private static string storageAccountConnectionString = JObject.Parse(connectionsContent)["STORAGE_ACCOUNT_SECRET_CONNECTION_STRING"].ToString();

        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
        private static CloudTable deviceStateTable = storageAccount.CreateCloudTableClient().GetTableReference("devicestate");

        [FunctionName("DeviceState")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            await deviceStateTable.CreateIfNotExistsAsync();

            var room = req.Headers["app"];
            if (string.IsNullOrEmpty(room))
            {
                room = req.Query["app"];
            }
            if (string.IsNullOrEmpty(room))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Please pass a room name on the query string or in the header")
                };
            }

            if (room.Equals("<your-app-name>") || room.Equals("test"))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent($"{room} is already taken, please enter an unique name")
                };
            }

            var partitionKey = "Example";
            var rowKey = room;

            try
            {
                // get the room from the table
                var getRoom = TableOperation.Retrieve<DeviceConfig>(partitionKey, rowKey);

                var query = await deviceStateTable.ExecuteAsync(getRoom);

                var currentConfig = (DeviceConfig)query.Result;

                // if current config does not exist, create a record using a default config
                if (currentConfig == null)
                {
                    var defaultRoom = new DeviceConfig(partitionKey, rowKey);
                    var createRoom = TableOperation.Insert(defaultRoom);
                    await deviceStateTable.ExecuteAsync(createRoom);
                    currentConfig = (DeviceConfig)(await deviceStateTable.ExecuteAsync(getRoom)).Result;
                }

                // handle GET request from Client application - get from Storage
                if (req.Method.Equals("get", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(currentConfig, Formatting.Indented), Encoding.UTF8, "application/json")
                    };
                }

                if (req.Method.Equals("post", StringComparison.OrdinalIgnoreCase))
                {
                    // handle POST request from Custom Commands - save to Storage
                    var item = req.Query["item"].ToString().ToLower();
                    var value = req.Query["value"].ToString().ToLower();

                    log.LogInformation($"item: {item}, value: {value}");

                    if (string.IsNullOrEmpty(item) || string.IsNullOrEmpty(value))
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent($"You need to pass \"item (SubjectDeviceName)\" and \"value(on or off)\" as parameters", Encoding.UTF8, "application/json")
                        };
                    }

                    switch (item)
                    {
                        case "tv":
                            currentConfig.TV = value;
                            break;
                        case "fan":
                            currentConfig.Fan = value;
                            break;
                        default:
                            return new HttpResponseMessage(HttpStatusCode.BadRequest)
                            {
                                Content = new StringContent($"The item is not available in your application. Please check the subjectDeviceNames which you have configured in your application", Encoding.UTF8, "application/json")
                            };
                    }

                    var updateRoom = TableOperation.Replace(currentConfig as DeviceConfig);
                    await deviceStateTable.ExecuteAsync(updateRoom);
                    log.LogInformation("successfully updated the record");

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(currentConfig, Formatting.Indented), Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
                {
                    Content = new StringContent($"Only GET and POST requests are allowed")
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
}
