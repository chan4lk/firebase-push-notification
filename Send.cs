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
using System.Text;

namespace Chan4lk.Notification
{
    public static class Send
    {
        [FunctionName("Send")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string to = req.Query["to"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            to = to ?? data?.to;
            await NotifyAsync(to,"From azure", "Sample message", log);
            return to != null
                ? (ActionResult)new OkObjectResult($"Send message to, {to}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        public static async Task<bool> NotifyAsync(string to, string title, string body, ILogger logger)
        {
            try
            {
                // Get the server key from FCM console
                var serverKey = string.Format("key={0}", "AIzaSyCgYSolr77E9rQpOPE7NrCjU6QOz6Atw4g");

                // Get the sender id from FCM console
                var senderId = string.Format("id={0}", "116236234431");

                var data = new
                {
                    to, // Recipient device token
                    notification = new { title, body }
                };

                // Using Newtonsoft.Json
                var jsonBody = JsonConvert.SerializeObject(data);

                using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send"))
                {
                    httpRequest.Headers.TryAddWithoutValidation("Authorization", serverKey);
                    httpRequest.Headers.TryAddWithoutValidation("Sender", senderId);
                    httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    using (var httpClient = new HttpClient())
                    {
                        var result = await httpClient.SendAsync(httpRequest);

                        if (result.IsSuccessStatusCode)
                        {
                            return true;
                        }
                        else
                        {
                            // Use result.StatusCode to handle failure
                            // Your custom error handler here
                            logger.LogError($"Error sending notification. Status Code: {result.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception thrown in Notify Service: {ex}");
            }

            return false;
        }
    }
}
