using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]


namespace EsepWebhook;


public class Function
{
    private static readonly HttpClient client = new HttpClient();


    ///
    /// A simple function that takes a string, extracts GitHub issue URL, and posts it to Slack.
    ///
    ///
    ///
    ///
    public string FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogInformation($"FunctionHandler received: {input}");


        try
        {
            // Deserialize the input to access the issue's html_url property
            var json = JsonConvert.DeserializeObject(input.ToString());


            // Safely access the html_url field within the issue object
            string issueUrl = json["issue"]?["html_url"]?.ToString();
            if (string.IsNullOrEmpty(issueUrl))
            {
                context.Logger.LogWarning("Issue URL not found in the input payload.");
                return "Issue URL not found.";
            }


            // Prepare the payload with the extracted issue link
            string payload = $"{{\"text\":\"Issue Created: {issueUrl}\"}}";


            // Get the Slack webhook URL from environment variables
            string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (string.IsNullOrEmpty(slackUrl))
            {
                context.Logger.LogError("SLACK_URL environment variable is not set.");
                return "SLACK_URL environment variable is not set.";
            }


            // Send the payload to Slack
            var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            
            var response = client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());
            
            // Return the response from Slack or an acknowledgment
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Exception occurred: {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }
}