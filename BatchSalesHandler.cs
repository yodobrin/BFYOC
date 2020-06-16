using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;


using BFYOC.Models;

namespace BFYOC
{
    public static class BatchSalesHandler
    {
        [FunctionName("BatchSalesHandler")]
        public static async Task Run([EventHubTrigger("pos-sales-events", Connection = "EH_CS")] EventData[] events, ILogger log)
        {
            var exceptions = new List<Exception>();
            string DatabaseName = Environment.GetEnvironmentVariable("COSMOS_DB_NAME");
            string CollectionName = Environment.GetEnvironmentVariable("COSMOS_SALES");
            string ConnectionStringSetting = Environment.GetEnvironmentVariable("COSMOS_CS");
            CosmosClient cosmosClient = new CosmosClient(ConnectionStringSetting);
            Container cosmosContainer = cosmosClient.GetContainer(DatabaseName,CollectionName);


            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");
                    dynamic salesEvents = JsonConvert.DeserializeObject(messageBody);
                    foreach (SalesEvent posevent in salesEvents)
                    {
                        string newId = Guid.NewGuid().ToString();
                        posevent.id = newId;
                        ItemResponse<SalesEvent> orderResponse = await cosmosContainer.CreateItemAsync<SalesEvent>(posevent, new PartitionKey(newId));
                        log.LogInformation($"insert a pos event with sales number: {posevent.header.salesNumber} to orders with id:{newId}");
                    }                
                    // await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
