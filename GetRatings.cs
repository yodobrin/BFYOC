using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;

using BFYOC.Models;

namespace BFYOC.Functions
{
    public static class GetRatings
    {
        [FunctionName("GetRatings")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get",  Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetRatings function processed a request.");

            string userid = req.Query["userId"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            userid = userid ?? data?.userId;

            string DatabaseName = Environment.GetEnvironmentVariable("COSMOS_DB_NAME");
            string CollectionName = Environment.GetEnvironmentVariable("COSMOS_COLLECTION");
            string ConnectionStringSetting = Environment.GetEnvironmentVariable("COSMOS_CS");

            CosmosClient cosmosClient = new CosmosClient(ConnectionStringSetting);
            Container cosmosContainer = cosmosClient.GetContainer(DatabaseName,CollectionName);

            var sqlQueryText = $"SELECT * FROM c WHERE c.userId = '{userid}'";
            QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
            FeedIterator<Rating> queryResultSetIterator = cosmosContainer.GetItemQueryIterator<Rating>(queryDefinition);

            List<Rating> ratings = new List<Rating>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<Rating> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (Rating rating in currentResultSet)
                {
                    ratings.Add(rating);
                }
            }
            string message = JsonConvert.SerializeObject(ratings);

            string responseMessage = $"got user id: {userid}:\n" + message;

            return new OkObjectResult(responseMessage);
        }
    }
}
