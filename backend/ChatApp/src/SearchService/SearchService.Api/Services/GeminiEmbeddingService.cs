using System.Linq;
using System.Text;
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
            _apiKey = configuration["Gemini:ApiKey"]
                ?? throw new ArgumentNullException("Gemini:ApiKey configuration is missing");
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<float>();

            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent";

            var payload = new
            {
                model = "models/gemini-embedding-001",
                content = new
                {
                    parts = new[] { new { text } }
                },
                outputDimensionality = 768
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-goog-api-key", _apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var resp = await _httpClient.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Error fetching embedding: {(int)resp.StatusCode} {resp.StatusCode}, {body}");

            var node = JsonNode.Parse(body);

            var values = node?["embedding"]?["values"]?.AsArray();
            if (values is null) return Array.Empty<float>();

            // đọc double rồi cast float cho an toàn
            return values
                .Select(v => (float)(v!.GetValue<double>()))
                .ToArray();
        }
    }
}