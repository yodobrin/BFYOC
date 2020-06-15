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

namespace BFYOC
{
    public static class OrderLanding
    {
        static string [] FILE_NAMES = {"OrderLineItems","OrderHeaderDetails","ProductInformation"};
        static string FILE_TYPE = ".csv";
        [FunctionName("OrderLanding")]
        public static async Task Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {            
            string connectionString = Environment.GetEnvironmentVariable("ORDER_LANDING_SA");
            string containerName = Environment.GetEnvironmentVariable("ORDER_LANDING_CONTAINER");
            dynamic eventContent = eventGridEvent.Data.ToString();
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
        }

        private static string GetUniqueId(string blobUrl,ILogger log)
        {
            //"url": "https://orderlanding.blob.core.windows.net/landing/20200615131600-ProductInformation.csv"
            string [] splitted = blobUrl.Split("/");
            int lenght = (splitted!=null)?splitted.Length:0;
            log.LogInformation($"trying to extract from {blobUrl} by index {lenght}");
            string [] uniques = splitted[lenght-1].Split("-");
            string uniq = uniques[0];
            log.LogInformation($"extracted f {uniq}");

            return uniq;
        }
    }
}
