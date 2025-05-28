using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebOptimus.Configuration;
using WebOptimus.Models;

namespace WebOptimus.Services
{
    

    public class PostmarkClient : IPostmarkClient
    {
        private readonly HttpClient httpClient;
        private readonly PostmarkOptions postmarkOptions;

        public PostmarkClient(HttpClient httpClient, IOptions<PostmarkOptions> postmarkOptions)
        {
            this.httpClient = httpClient;
            httpClient.BaseAddress = new Uri("https://api.postmarkapp.com");
            this.postmarkOptions = postmarkOptions.Value;
        }

        public async Task<PostmarkStatus> SendMessageAsync(PostmarkMessage message, CancellationToken ct)
        {
            try
            {
                if (!httpClient.DefaultRequestHeaders.Any())
                {
                    httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
                    httpClient.DefaultRequestHeaders.Add("X-Postmark-Server-Token", postmarkOptions.ApiKey);
                }
                var jsonOpts = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                };

                var jsonObj = JsonSerializer.Serialize(message, jsonOpts);
                var stringContent = new StringContent(jsonObj, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/email", stringContent, ct).ConfigureAwait(false);

                return response.IsSuccessStatusCode ? PostmarkStatus.Success : PostmarkStatus.Failed;
            }
            catch
            {
                return PostmarkStatus.Failed;
            }
        }

        public async Task<List<(string To, PostmarkStatus Status)>> SendBatchMessagesAsync(List<PostmarkMessage> messages, CancellationToken ct)
        {
            var jsonOpts = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            var jsonObj = JsonSerializer.Serialize(new { Messages = messages }, jsonOpts);
            var stringContent = new StringContent(jsonObj, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/email/batch", stringContent, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // If batch failed completely, return all as failed
                return messages.Select(m => (m.To, PostmarkStatus.Failed)).ToList();
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);

            // Deserialize response assuming Postmark returns an array of status objects
            var batchResponse = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(responseContent, jsonOpts);

            return batchResponse?.Select((res, index) => (
                messages[index].To,
                res.ContainsKey("ErrorCode") && (int)res["ErrorCode"] == 0
                    ? PostmarkStatus.Success
                    : PostmarkStatus.Failed
            )).ToList() ?? messages.Select(m => (m.To, PostmarkStatus.Failed)).ToList();
        }

    }
}
