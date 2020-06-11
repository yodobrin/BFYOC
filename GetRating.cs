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
    public static class GetRating 
    {
        [FunctionName("GetRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,            
            ILogger log)
        {
            log.LogInformation("GetRating function processed a request.");
            
            string DatabaseName = Environment.GetEnvironmentVariable("COSMOS_DB_NAME");
            string CollectionName = Environment.GetEnvironmentVariable("COSMOS_COLLECTION");
            string ConnectionStringSetting = Environment.GetEnvironmentVariable("COSMOS_CS");

            CosmosClient cosmosClient = new CosmosClient(ConnectionStringSetting);
            Container cosmosContainer = cosmosClient.GetContainer(DatabaseName,CollectionName);
            string ratingid = req.Query["ratingId"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            ratingid = ratingid ?? data?.ratingid;
            var sqlQueryText = $"SELECT * FROM c WHERE c.id = '{ratingid}'";
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
            if(ratings.Count == 0)
            {
                // not found
                return new NotFoundObjectResult($"No rating with id:{ratingid} was found");
            }
            // should not get here if no results
            string message = JsonConvert.SerializeObject(ratings[0]);

            string responseMessage = $"got rating: {ratingid}.\n" + message;

            return new OkObjectResult(responseMessage);
        }
    }
}
