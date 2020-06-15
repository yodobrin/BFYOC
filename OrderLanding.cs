// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;

namespace BFYOC
{
    public static class OrderLanding
    {
        static string [] FILE_NAMES = {"OrderLineItems","OrderHeaderDetails","ProductInformation"};
        static string FILE_TYPE = ".csv";
        [FunctionName("OrderLanding")]
        public static void Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {            
            string connectionString = Environment.GetEnvironmentVariable("ORDER_LANDING_SA");
            string containerName = Environment.GetEnvironmentVariable("ORDER_LANDING_CONTAINER");
            string eventContent = eventGridEvent.Data.ToString();
            log.LogInformation(eventContent);
        }
    }
}
