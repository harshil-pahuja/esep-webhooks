using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Amazon.Lambda.APIGatewayEvents;
using System.Net.Http;
using System.IO;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        /// <summary>
        /// A function that extracts the issue URL from the GitHub webhook payload.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public string FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
        {
            context.Logger.LogInformation("FunctionHandler received webhook data.");

            // Deserialize the JSON payload from the request body
            dynamic json = JsonConvert.DeserializeObject(input.Body);
            
            // Extract the issue URL
            string issueUrl = json.issue?.html_url;
            if (issueUrl == null)
            {
                context.Logger.LogError("No issue URL found in the webhook payload.");
                return "Error: Issue URL not found in payload.";
            }

            // Create payload for the Slack message
            string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });

            // Send the payload to Slack
            using var client = new HttpClient();
            var webRequest = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable("SLACK_URL"))
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var response = client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());
                
            return reader.ReadToEnd();
        }
    }
}
