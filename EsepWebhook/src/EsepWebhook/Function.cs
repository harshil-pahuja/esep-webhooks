using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Amazon.Lambda.APIGatewayEvents;
using System.Net.Http;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// A function that sends a Slack message with the GitHub issue URL when triggered by a GitHub webhook event.
        /// </summary>
        /// <param name="input">The API Gateway request containing the GitHub webhook payload.</param>
        /// <param name="context">The Lambda context.</param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
        {
            context.Logger.LogInformation("FunctionHandler received webhook data.");

            // Parse the JSON payload from GitHub
            dynamic json = JsonConvert.DeserializeObject(input.Body);
            
            // Extract the issue URL
            string issueUrl = json?.issue?.html_url;
            if (string.IsNullOrEmpty(issueUrl))
            {
                context.Logger.LogError("No issue URL found in the webhook payload.");
                return "Error: Issue URL not found in payload.";
            }

            // Create the Slack message payload
            string slackMessage = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });

            // Send the message to the Slack webhook URL
            string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
            {
                Content = new StringContent(slackMessage, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(webRequest);
            if (!response.IsSuccessStatusCode)
            {
                context.Logger.LogError($"Failed to send message to Slack. Status code: {response.StatusCode}");
                return "Error: Failed to send message to Slack.";
            }

            context.Logger.LogInformation("Successfully posted message to Slack.");
            return "Message posted to Slack successfully.";
        }
    }
}
