using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System.Net.Http;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        public string FunctionHandler(object input, ILambdaContext context)
        {
            context.Logger.LogInformation("FunctionHandler received an event.");

            // Retrieve Slack URL from environment variables
            var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (string.IsNullOrEmpty(slackUrl))
            {
                context.Logger.LogError("SLACK_URL environment variable is not set.");
                throw new Exception("SLACK_URL environment variable is not set.");
            }

            // Attempt to parse JSON and extract the issue URL
            dynamic json;
            try
            {
                json = JsonConvert.DeserializeObject<dynamic>(input.ToString());

                // Verify that 'html_url' exists in the 'issue' object
                if (json?.issue?.html_url == null)
                {
                    context.Logger.LogError("Invalid payload: 'html_url' is missing in the 'issue' object.");
                    throw new Exception("Invalid payload: 'html_url' is missing in the 'issue' object.");
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError("Error parsing JSON input: " + ex.Message);
                throw new Exception("Error parsing JSON input: " + ex.Message);
            }

            // Use the html_url from the payload
            string htmlUrl = json.issue.html_url;
            context.Logger.LogInformation($"Found issue URL: {htmlUrl}");

            // Create payload for Slack
            string payload = $"{{\"text\":\"Issue Created: {htmlUrl}\"}}";
            context.Logger.LogInformation("Payload created for Slack: " + payload);

            // Send payload to Slack
            using var client = new HttpClient();
            var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var response = client.Send(webRequest);
            using var reader = new StreamReader(response.Content.ReadAsStream());

            return reader.ReadToEnd();
        }
    }
}
