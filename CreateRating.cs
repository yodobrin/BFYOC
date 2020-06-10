using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;

using BFYOC.Models;

// {
//   "userId": "cc20a6fb-a91f-4192-874d-132493685376",
//   "productId": "4c25613a-a3c2-4ef3-8e02-9c335eb23204",
//   "locationName": "Sample ice cream shop",
//   "rating": 5,
//   "userNotes": "I love the subtle notes of orange in this ice cream!"
// }
namespace BFYOC.Functions
{
    public static class CreateRating
    {
        static string ValidateUserURI = Environment.GetEnvironmentVariable("GET_USER_EP");
        static string ValidateProductURI = Environment.GetEnvironmentVariable("GET_PRODUCT_EP");

        [FunctionName("CreateRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CreateRating function processed a request.");

            string DatabaseName = Environment.GetEnvironmentVariable("COSMOS_DB_NAME");
            string CollectionName = Environment.GetEnvironmentVariable("COSMOS_COLLECTION");
            string ConnectionStringSetting = Environment.GetEnvironmentVariable("COSMOS_CS");

            CosmosClient cosmosClient = new CosmosClient(ConnectionStringSetting);
            Container cosmosContainer = cosmosClient.GetContainer(DatabaseName,CollectionName);

            // string name = req.Query["name"];
            var aRating = new Rating();
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"CreateRating : got string data: {requestBody}");
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            log.LogInformation("CreateRating : got dynamic data");
            aRating.userId = data?.userId;
            aRating.productId = data?.productId;
            aRating.locationName = data?.locationName;
            string s_rating = data?.rating;
            aRating.rating = (string.IsNullOrEmpty(s_rating))? 0: int.Parse(s_rating);
            aRating.userNotes = data?.userNotes;
            aRating.timestamp = new DateTime();
            aRating.id = Guid.NewGuid().ToString();
            Boolean ValidUser = await ValidateUserId(aRating.userId,log);
            Boolean ValidProduct = await ValidateProductId(aRating.productId,log);

            if(!ValidUser || !ValidProduct || !ValidateRating(aRating.rating))
            {
                string errorresponse = "One or more items does not exist, please try again";
                return new NotFoundObjectResult(errorresponse);
            }
            string okresponse = JsonConvert.SerializeObject(aRating);

            ItemResponse<Rating> ratingResponse = await cosmosContainer.CreateItemAsync<Rating>(aRating, new PartitionKey(aRating.id));                        

            return new OkObjectResult(okresponse);
        }

        private static async Task<Boolean> ValidateUserId(String userid,ILogger log)
        {
            HttpClient client = new HttpClient();
            string path = ValidateUserURI + $"?userId={userid}";
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                log.LogInformation($"CreateRating got from user: {content}");
                dynamic data = JsonConvert.DeserializeObject(content);
                if(data!=null && data?.userName!=null) return true;
                else return false;
                // return true;
            }
            else return false;
        }

        private static async Task<Boolean> ValidateProductId(String productid,ILogger log)
        {
            HttpClient client = new HttpClient();
            string path = ValidateProductURI + $"?productId={productid}";
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                log.LogInformation($"CreateRating got from product: {content}");
                dynamic data = JsonConvert.DeserializeObject(content);
                if(data!=null && data?.productId!=null) return true;
                else return false;
                // return true;
            }
            else return false;
        }

        private static Boolean ValidateRating(int rating)
        {
            if (rating>0 && rating<=5) return true;
            else return false;
        }
    }
}
