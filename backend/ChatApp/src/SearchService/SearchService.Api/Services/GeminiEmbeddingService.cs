using System.Text.Json;
using System.Text.Json.Nodes;

namespace SearchService.Api.Services
{
    public interface IEmbeddingService
    {
        Task<float[]> GetEmbeddingAsync(string text);
    }
    public class GeminiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        public GeminiEmbeddingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApiKey"] ?? throw new ArgumentNullException("GeminiApiKey configuration is missing");
        }
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<float>();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={_apiKey}";
            //body sent to the API
            var payload = new
            {
                model = "models/text-embedding-004",
                content = new { parts = new[] { new { text = text } } }
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error fetching embedding: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            var node = JsonNode.Parse(responseContent);

            var value = node?["embedding"]?["values"]?.AsArray();

            if (value == null) return Array.Empty<float>();
            return value.Select(v => v!.GetValue<float>()).ToArray();
        }
    }
}
