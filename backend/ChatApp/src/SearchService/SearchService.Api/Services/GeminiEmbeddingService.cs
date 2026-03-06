using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SearchService.Api.Services
{
    public interface IEmbeddingService
    {
        Task<float[]> GetEmbeddingAsync(string text);
        Task<string> SummarizeChatAsync(string chatHistory);
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
                content = new { parts = new[] { new { text } } },
                outputDimensionality = 768
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-goog-api-key", _apiKey); 
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var resp = await _httpClient.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Error fetching embedding: {body}");

            var node = JsonNode.Parse(body);
            var values = node?["embedding"]?["values"]?.AsArray();

            return values?.Select(v => (float)v!.GetValue<double>()).ToArray() ?? Array.Empty<float>();
        }

        public async Task<string> SummarizeChatAsync(string chatHistory)
        {
            if (string.IsNullOrWhiteSpace(chatHistory)) return "Không có nội dung để tóm tắt.";

            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

            var prompt = $@"
            Bạn là một trợ lý tóm tắt hội thoại chuyên nghiệp. 
            Dưới đây là các tin nhắn mới trong một nhóm chat mà người dùng chưa đọc.
            Hãy tóm tắt nội dung theo các tiêu chí sau:
            - Chủ đề chính mọi người đang thảo luận.
            - Có ai nhắc đến hoặc đặt câu hỏi trực tiếp cho người dùng không?
            - Các quyết định hoặc lịch trình quan trọng đã được chốt.
            
            Yêu cầu: Viết ngắn gọn, súc tích bằng Tiếng Việt, sử dụng bullet points.
            
            Nội dung tin nhắn:
            {chatHistory}";

            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new
                {
                    maxOutputTokens = 500,
                    temperature = 0.5
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-goog-api-key", _apiKey); 
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var resp = await _httpClient.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Gemini Summarization Error: {body}");

            var node = JsonNode.Parse(body);

            // Lấy text trả về từ cấu trúc generateContent
            var summary = node?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();

            return summary ?? "Không có nội dung mới để tóm tắt.";
        }
    }
}