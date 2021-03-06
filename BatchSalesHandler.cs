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

using Microsoft.Azure.ServiceBus;


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

                    log.LogInformation($"BatchSalesHandlerfunction processed a message: {messageBody}");
                    dynamic salesEvent = JsonConvert.DeserializeObject(messageBody);
                    string newId = Guid.NewGuid().ToString();
                    salesEvent.id = newId;
                    ItemResponse<Object> orderResponse = await cosmosContainer.CreateItemAsync<Object>(salesEvent, new PartitionKey(newId));
                    log.LogInformation($"BatchSalesHandlerfunction:insert a pos event with id:{newId}");
                    string res = await SendToReceiptTopic(salesEvent,log);
                    log.LogInformation($"BatchSalesHandlerfunction: called send to receipt with result {res}");
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

        public static async Task<string> SendToReceiptTopic(dynamic salesEvent, ILogger log)
        {
            log.LogInformation("BatchSalesHandler:SendToReceiptTopic");
            string pdfpath = salesEvent?.header.receiptUrl;
            if(String.IsNullOrEmpty(pdfpath)) return "no receipt";
            string ServiceBusConnectionString = Environment.GetEnvironmentVariable("SB_TOPIC_CS");;
            string TopicName = Environment.GetEnvironmentVariable("SB_TOPIC_NAME");;
            ITopicClient topicClient = new TopicClient(ServiceBusConnectionString, TopicName);
            // string messageBody = "{god dam}";
            dynamic receipt = new System.Dynamic.ExpandoObject();
            dynamic products = salesEvent?.details;

            receipt.totalItems = CountProducts(products);
            receipt.totalCost = CalcCost(salesEvent?.header.totalCost);
            receipt.salesNumber = salesEvent?.header.salesNumber;
            receipt.salesDate = salesEvent?.header.dateTime;
            receipt.storeLocation = salesEvent?.header.locationId;
            receipt.receiptUrl = salesEvent?.header.receiptUrl;
            
            string fuck = JsonConvert.SerializeObject(receipt);
            log.LogInformation($"BatchSalesHandler sending to topic {TopicName} the message:{fuck}");
            var message = new Message(Encoding.UTF8.GetBytes(fuck));
            message.UserProperties.Add("totalCost",receipt.totalCost);
            message.ContentType = System.Net.Mime.MediaTypeNames.Application.Json;
            await topicClient.SendAsync(message);

            return "receipt attached.";
        }
        private static double CalcCost(dynamic cost)
        {
            string scost = cost.ToString();
            if(string.IsNullOrEmpty(scost)) return 0;
            return double.Parse(scost);
        }
        private static int CountProducts(dynamic products)
        {
            int counter = 0;
            foreach (dynamic product in products) counter++;
            return counter;
        }
    }
}
