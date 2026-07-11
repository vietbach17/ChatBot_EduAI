using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _defaultModel;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var key = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                      ?? configuration["GeminiAI:ApiKey"];
            
            _apiKey = key?.Trim() ?? throw new ArgumentNullException("GEMINI_API_KEY is not configured");
            
            _defaultModel = Environment.GetEnvironmentVariable("GEMINI_MODEL") 
                            ?? configuration["GeminiAI:Model"] 
                            ?? "gemini-1.5-flash";
        }

        public async Task<string> GenerateJsonContentAsync(string prompt, string? responseSchemaJson = null)
        {
            var model = Environment.GetEnvironmentVariable("GEMINI_MODEL")?.Trim();
            if (string.IsNullOrEmpty(model))
            {
                model = "gemini-2.5-flash"; // Mô hình mặc định ổn định, hỗ trợ JSON Schema
            }
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";

            object? schemaObj = null;
            if (!string.IsNullOrEmpty(responseSchemaJson))
            {
                schemaObj = JsonSerializer.Deserialize<object>(responseSchemaJson);
            }

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = schemaObj
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var requestBody = JsonSerializer.Serialize(payload, jsonOptions);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Gemini API error: {response.StatusCode} - {errorText}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("candidates", out var candidates) && 
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var candidateContent) &&
                candidateContent.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var text))
            {
                return text.GetString() ?? string.Empty;
            }

            throw new InvalidOperationException($"Không thể phân tích kết quả trả về từ Gemini API. Response: {responseBody}");
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={_apiKey}";
            
            var requestBody = new
            {
                model = "models/gemini-embedding-001",
                content = new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API Error: {response.StatusCode} - {errorBody}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseString);

            var values = result.GetProperty("embedding").GetProperty("values").EnumerateArray()
                .Select(x => x.GetSingle())
                .Take(768)
                .ToArray();

            return values;
        }

        public async Task<List<float[]>> GetEmbeddingsAsync(List<string> texts)
        {
            var results = new List<float[]>();
            foreach (var text in texts)
            {
                results.Add(await GetEmbeddingAsync(text));
            }
            return results;
        }

        public async Task<string> GenerateAnswerAsync(string prompt, string? modelName = null)
        {
            modelName = string.IsNullOrWhiteSpace(modelName) ? _defaultModel : modelName;
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var result = JsonDocument.Parse(responseString);
                    var candidates = result.RootElement.GetProperty("candidates");
                    if (candidates.GetArrayLength() == 0) return string.Empty;
                    var parts = candidates[0].GetProperty("content").GetProperty("parts");
                    if (parts.GetArrayLength() == 0) return string.Empty;
                    return parts[0].GetProperty("text").GetString() ?? string.Empty;
                }

                var statusCode = (int)response.StatusCode;

                // Retry trên 429 hoặc 503
                if ((statusCode == 429 || statusCode == 503) && attempt < maxRetries)
                {
                    var delay = attempt * 6000; // 6s, 12s
                    await Task.Delay(delay);
                    continue;
                }

                // Trả về thông báo thân thiện thay vì dump lỗi kỹ thuật
                if (statusCode == 429)
                    return " Trợ lý AI hiện đang quá tải, vui lòng thử lại sau vài giây.";
                if (statusCode == 503)
                    return " Dịch vụ AI tạm thời không khả dụng, vui lòng thử lại sau.";

                var errorBody = await response.Content.ReadAsStringAsync();
                return $"Lỗi khi gọi AI: {response.StatusCode} - {errorBody}";
            }

            return " Không thể kết nối đến dịch vụ AI sau nhiều lần thử. Vui lòng thử lại sau.";
        }

        public async Task<List<GeminiModelInfo>> GetAvailableModelsAsync()
        {
            var defaultList = new List<GeminiModelInfo>
            {
                new("gemini-3.5-flash", "Gemini 3.5 Flash"),
                new("gemini-2.0-flash", "Gemini 2.0 Flash"),
                new("gemini-2.0-flash-lite", "Gemini 2.0 Flash Lite"),
                new("gemini-1.5-pro", "Gemini 1.5 Pro"),
                new("gemini-1.5-flash", "Gemini 1.5 Flash"),
                new("gemini-pro", "Gemini Pro"),
            };

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={_apiKey}&pageSize=50";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return defaultList;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);

                var models = new List<GeminiModelInfo>();
                foreach (var model in doc.RootElement.GetProperty("models").EnumerateArray())
                {
                    var name = model.GetProperty("name").GetString() ?? string.Empty; // "models/gemini-..."
                    var id = name.Replace("models/", "");

                    // Chỉ lấy các model hỗ trợ generateContent
                    bool supportsGenerate = false;
                    if (model.TryGetProperty("supportedGenerationMethods", out var methods))
                    {
                        foreach (var m in methods.EnumerateArray())
                        {
                            if (m.GetString() == "generateContent") { supportsGenerate = true; break; }
                        }
                    }
                    if (!supportsGenerate) continue;

                    var displayName = model.TryGetProperty("displayName", out var dn)
                        ? dn.GetString() ?? id
                        : id;

                    models.Add(new GeminiModelInfo(id, displayName));
                }

                return models.Count > 0 ? models : defaultList;
            }
            catch
            {
                return defaultList;
            }
        }
    }
}
