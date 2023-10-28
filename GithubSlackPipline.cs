using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AssigmentDataBase.Models;
using Newtonsoft.Json;
using System.Net;

namespace AssigmentDataBase
{
    public class GithubSlackPipline
    {
        private readonly ILogger _logger;

        public GithubSlackPipline(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GithubSlackPipline>();
        }
       

        [Function("GithubSlackPipline")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            string name)
        {
            _logger.LogInformation("Github-Slack pipeline has been initiated.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<GithubHook>(requestBody);

            if (data != null && data.commits != null && data.commits.Count > 0)
            {
                var messageToSlack = $"New commit has been pushed. Commit ID: {data.commits[0].id} Message: {data.commits[0].message}";

                await SendSlackMessage(messageToSlack);

                _logger.LogInformation("Github-Slack pipeline has completed successfully.");

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString(messageToSlack);
                
                return response;
            }
            else
            {
                _logger.LogError("Invalid data received. No commits found.");
                // Handle the case when data or commits are null or empty, you can return an appropriate response or take other actions.
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString("Invalid data received.");
                return response;
            }
        }

        // Slack hook
        public static async Task<string> SendSlackMessage(string message)
        {
            try
            {
                var urlWebhook = "https://hooks.slack.com/services/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";

                using (var client = new HttpClient())
                {
                    var jsonBody = "{'text' : '" + message + "' }";
                    var data = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(urlWebhook, data);

                    var result = await response.Content.ReadAsStringAsync();

                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }
    }
}