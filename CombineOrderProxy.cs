using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
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

namespace BFYOC
{
    public static class CombineOrderProxy
    {
        [FunctionName("CombineOrderProxy")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CombineOrderProxy function processed a request.");          

            string requestBody = await new StreamReader(req.Body,Encoding.UTF8).ReadToEndAsync();

            //string response = await CallCombine(requestBody,log);    

            string DatabaseName = Environment.GetEnvironmentVariable("COSMOS_DB_NAME");
            string CollectionName = Environment.GetEnvironmentVariable("COSMOS_SALES");
            string ConnectionStringSetting = Environment.GetEnvironmentVariable("COSMOS_CS");
            CosmosClient cosmosClient = new CosmosClient(ConnectionStringSetting);
            Container cosmosContainer = cosmosClient.GetContainer(DatabaseName,CollectionName);

            dynamic orders = JsonConvert.DeserializeObject(requestBody);
            string newId = Guid.NewGuid().ToString();
            orders.id = newId;
            ItemResponse<Object> orderResponse = await cosmosContainer.CreateItemAsync<Object>(orders, new PartitionKey(newId));
            log.LogInformation($"events are: {orders.ToString()}");
            // foreach (dynamic order in orders)
            // {
            //     string newId = Guid.NewGuid().ToString();
            //     //order.id = newId;
            //     log.LogInformation($"event is: {order.ToString()}");
            //     ItemResponse<Object> orderResponse = await cosmosContainer.CreateItemAsync<Object>(order, new PartitionKey(newId));
            //     // log.LogInformation($"insert an order {order?.salesNumber} to orders");
            // }                    

            return new OkObjectResult("cos emek");
        }

        private static async Task<string> CallCombine(string payload, ILogger log)
        {
            HttpClient client = new HttpClient();
            string combineUrl = Environment.GetEnvironmentVariable("COMBINE_URI");
            client.BaseAddress = new Uri(combineUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
            StringContent content = new StringContent(payload,Encoding.UTF8,"application/json");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post,"");
            request.Content = content;
            HttpResponseMessage resp = await client.SendAsync(request);
            Stream receiveStream = await resp.Content.ReadAsStreamAsync();
            StreamReader readStream = new StreamReader (receiveStream, Encoding.UTF8);
            string res = readStream.ReadToEnd();
            log.LogInformation($"got resp={resp.StatusCode}");
            log.LogInformation($"got json?={res}");

            return res;

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
            log.LogInformation($"BatchSalesHandler calling uri: {funcurl} ");
            HttpResponseMessage response = await client.SendAsync(request);
            Stream receiveStream = await response.Content.ReadAsStreamAsync();
            StreamReader readStream = new StreamReader (receiveStream, Encoding.UTF8);
            string responseString = readStream.ReadToEnd();            
                        
            log.LogInformation($"BatchSalesHandler: got response status: {response.StatusCode}");
            
            log.LogInformation($"BatchSalesHandler got in string:{responseString}");
            
            return responseString;
            
        }
    }
    
}
