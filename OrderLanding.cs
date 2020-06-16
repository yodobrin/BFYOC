// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.IO;
using Microsoft.Azure.Cosmos;

namespace BFYOC
{
    public static class OrderLanding
    {
        [FunctionName("OrderLanding")]
        public static async Task Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {            
            string connectionString = Environment.GetEnvironmentVariable("ORDER_LANDING_SA");
            string containerName = Environment.GetEnvironmentVariable("ORDER_LANDING_CONTAINER");
            dynamic eventContent = JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
            string blobUrl = eventContent?.url;
            log.LogInformation(blobUrl);
            string unique = GetUniqueId(blobUrl,log);

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);  

            
            int counter = 0;
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.None,Azure.Storage.Blobs.Models.BlobStates.None,unique))
            {
                log.LogInformation($"inner loop of blobs: {blobItem.Name}");
                counter ++;
            }
            log.LogInformation($"counted {counter} files with the unique {unique}");
            string result = "";
            if(counter == 3)
            {
                log.LogInformation("Order landing - found 3 files calling the combine");

                result = await CallCombine(unique,log);

                log.LogInformation($"got combined result: {result}");
            }else return;
            string DatabaseName = Environment.GetEnvironmentVariable("COSMOS_DB_NAME");
            string CollectionName = Environment.GetEnvironmentVariable("COSMOS_ORDERS");
            string ConnectionStringSetting = Environment.GetEnvironmentVariable("COSMOS_CS");
            CosmosClient cosmosClient = new CosmosClient(ConnectionStringSetting);
            Container cosmosContainer = cosmosClient.GetContainer(DatabaseName,CollectionName);

            dynamic orders = JsonConvert.DeserializeObject(result);
            foreach (dynamic order in orders)
            {
                string newId = Guid.NewGuid().ToString();
                order.id = newId;
                ItemResponse<Object> orderResponse = await cosmosContainer.CreateItemAsync<Object>(order, new PartitionKey(newId));
                log.LogInformation($"insert an order with sales number: {order?.salesNumber} to orders with id:{newId}");
            }

        }

     

        private static string GetUniqueId(string blobUrl,ILogger log)
        {
            string [] splitted = blobUrl.Split("/");
            int lenght = (splitted!=null)?splitted.Length:0;
            log.LogInformation($"trying to extract from {blobUrl} by index {lenght}");
            string [] uniques = splitted[lenght-1].Split("-");
            string uniq = uniques[0];
            log.LogInformation($"extracted file {uniq}");

            return uniq;
        }

        private static string CreateCombineRequest(string unique, ILogger log)
        {
            var jsonRequestString = new
            {
                orderHeaderDetailsCSVUrl = $"https://orderlanding.blob.core.windows.net/landing/{unique}-OrderHeaderDetails.csv",
                orderLineItemsCSVUrl = $"https://orderlanding.blob.core.windows.net/landing/{unique}-OrderLineItems.csv",
                productInformationCSVUrl = $"https://orderlanding.blob.core.windows.net/landing/{unique}-ProductInformation.csv",
            };
            return JsonConvert.SerializeObject(jsonRequestString);                        
        }

        private static async Task<string> CallCombine(string unique, ILogger log)
        {
            HttpClient client = new HttpClient();
            string combineUrl = Environment.GetEnvironmentVariable("COMBINE_URI");
            client.BaseAddress = new Uri(combineUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header            
            var payload = CreateCombineRequest(unique,log);

            StringContent content = new StringContent(payload,Encoding.UTF8,"application/json");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,"");
            request.Content = content;
            log.LogInformation($"calling uri: {combineUrl} with payload: {content.ToString()}");
            HttpResponseMessage response = await client.SendAsync(request);
            Stream receiveStream = await response.Content.ReadAsStreamAsync();
            StreamReader readStream = new StreamReader (receiveStream, Encoding.UTF8);
            string responseString = readStream.ReadToEnd();            
                        
            log.LogInformation($"got response: {response.StatusCode}");
            
            log.LogInformation($"got in combine:{responseString}");
            
            return responseString;
            
        }
    }
}
