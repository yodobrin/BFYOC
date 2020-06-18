using System;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using System.Text;

namespace BFYOC
{
    public static class ChangeFeedOrders
    {
        [FunctionName("ChangeFeedOrders")]
        public static void Run([CosmosDBTrigger(
            databaseName: "openhack",
            collectionName: "orders",
            ConnectionStringSetting = "COSMOS_CS",
            LeaseCollectionName = "leases",CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<Document> documents, ILogger log)
        {
            if (documents != null && documents.Count > 0)
            {
                log.LogInformation("Documents modified " + documents.Count);
                log.LogInformation("First document Id " + documents[0].Id);
                string connectionString = Environment.GetEnvironmentVariable("EH_CS");
                string EventHubName = Environment.GetEnvironmentVariable("EH_CF_ORDERS");

                EventHubsConnectionStringBuilder eventHubConnectionStringBuilder =
                new EventHubsConnectionStringBuilder(connectionString)
                {
                    EntityPath = EventHubName
                };

                EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(eventHubConnectionStringBuilder.ToString());

                // Iterate through modified documents from change feed.
                foreach (var doc in documents)
                {
                    // Convert documents to json.
                    string json = JsonConvert.SerializeObject(doc);
                    EventData data = new EventData(Encoding.UTF8.GetBytes(json));

                    // Use Event Hub client to send the change events to event hub.
                    eventHubClient.SendAsync(data);
                }
            }
        }
    }
}
