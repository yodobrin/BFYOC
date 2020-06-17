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

namespace BFYOC
{
    public static class Pdf2Base64
    {
        [FunctionName("Pdf2Base64")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Pdf2Base64 processed a request.");

            string pdfpath = req.Query["pdfurl"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            pdfpath = pdfpath ?? data?.pdfurl;
            
            WebClient client = new WebClient();
            string localfile = $"{Guid.NewGuid().ToString()}.pdf";
            client.DownloadFile(new Uri(pdfpath),localfile);
            log.LogInformation($"Pdf2Base64 downloaded local file {localfile}");
            byte [] content = File.ReadAllBytes(localfile);
            string base64 = Convert.ToBase64String(content);
            log.LogInformation($"Pdf2Base64 converted local file size {content.Length} in bytes");

            return new OkObjectResult(base64);
        }
    }
}
