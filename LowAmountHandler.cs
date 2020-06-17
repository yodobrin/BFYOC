using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Azure.Cosmos;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

// {
//     "Store": "00d8ea6f-935c-2cca-9bbc-f56b5a091621",
//     "SalesNumber": "0c423398-3c7c-0682-7519-4701c445ed7a",
//     "TotalCost": 6.58,
//     "Items": 1,
//     "SalesDate": "09/02/2019 10:36:17"
// }
namespace BFYOC
{
    public static class LowAmountHandler
    {
        [FunctionName("LowAmountHandler")]
        public static async Task Run([ServiceBusTrigger("receipts", "low-amount", Connection = "SB_TOPIC_CS")]string mySbMsg, ILogger log)
        {
            log.LogInformation($"LowAmountHandler processed message: {mySbMsg}");
            dynamic salesEvent = JsonConvert.DeserializeObject(mySbMsg);
            
            
            dynamic tosave = new System.Dynamic.ExpandoObject();
            tosave.Store = salesEvent?.storeLocation;
            tosave.SalesNumber = salesEvent?.salesNumber;
            tosave.TotalCost = salesEvent?.totalCost;
            tosave.Items = salesEvent?.totalItems;
            tosave.SalesDate = salesEvent?.salesDate;
            

            string blobcontent = JsonConvert.SerializeObject(tosave);
            log.LogInformation($"LowAmountHandler blob to be saved with content {blobcontent}");

            string connectionString = Environment.GetEnvironmentVariable("SEC_SA_CS");
            string containerName = Environment.GetEnvironmentVariable("SEC_SA_CONTAINER_LOW");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName); 
            string blobName = $"{tosave.SalesNumber}";
            System.IO.MemoryStream blobstreamcontent = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(blobcontent));

            await containerClient.UploadBlobAsync(blobName,blobstreamcontent);
        }
    }
}
