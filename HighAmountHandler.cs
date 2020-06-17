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

using System.Web;

using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;


namespace BFYOC
{
    public static class HighAmountHandler
    {
        [FunctionName("HighAmountHandler")]
        public static async Task Run([ServiceBusTrigger("receipts", "high", Connection = "SB_TOPIC_CS")]string mySbMsg, ILogger log)
        {
            log.LogInformation($"HighAmountHandler processed message: {mySbMsg}");
            dynamic salesEvent = JsonConvert.DeserializeObject(mySbMsg);
            string pdfpath = salesEvent?.receiptUrl;
            string base64 = await CallPdf2Base64(pdfpath,log);
            dynamic tosave = new System.Dynamic.ExpandoObject();
            tosave.Store = salesEvent?.storeLocation;
            tosave.SalesNumber = salesEvent?.salesNumber;
            tosave.TotalCost = CalcCost(salesEvent?.totalCost);
            tosave.Items = salesEvent?.totalItems;
            tosave.SalesDate = salesEvent?.salesDate;
            tosave.ReceiptImage = base64;

            string blobcontent = JsonConvert.SerializeObject(tosave);
            log.LogInformation($"HighAmountHandler blob to be saved with content {blobcontent}");

            string connectionString = Environment.GetEnvironmentVariable("SEC_SA_CS");
            string containerName = Environment.GetEnvironmentVariable("SEC_SA_CONTAINER_HIGE");
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName); 
            string blobName = $"{tosave.SalesNumber}";
            System.IO.MemoryStream blobstreamcontent = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(blobcontent));

            await containerClient.UploadBlobAsync(blobName,blobstreamcontent);
        }

        private static double CalcCost(dynamic cost)
        {
            string scost = cost.ToString();            
            if(string.IsNullOrEmpty(scost)) return 0;
            return double.Parse(scost);
        }
        private static async Task<string> CallPdf2Base64(string pdfpath, ILogger log)
        {
            HttpClient client = new HttpClient();
            string baseurl = Environment.GetEnvironmentVariable("PDF2BASE64_URI");
            string code = Environment.GetEnvironmentVariable("PDF2BASE64_CODE");
            
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header            

            // StringContent content = new StringContent(payload,Encoding.UTF8,"application/json");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,"");
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["code"] = code;
            query["pdfurl"] = pdfpath;
            
            string funcurl = $"{baseurl}?{query.ToString()}";

            client.BaseAddress = new Uri(funcurl);
            log.LogInformation($"CallPdf2Base64 calling uri: {funcurl} ");
            HttpResponseMessage response = await client.SendAsync(request);
            Stream receiveStream = await response.Content.ReadAsStreamAsync();
            StreamReader readStream = new StreamReader (receiveStream, Encoding.UTF8);
            string responseString = readStream.ReadToEnd();            
                        
            log.LogInformation($"CallPdf2Base64: got response status: {response.StatusCode}");
            
            log.LogInformation($"CallPdf2Base64 got in string:{responseString}");
            
            return responseString;
            
        }
    }
}
