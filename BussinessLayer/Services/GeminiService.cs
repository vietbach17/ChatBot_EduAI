using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
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

        // ─── GenerateJsonContent ───────────────────────────────────────────────
        public async Task<string> GenerateJsonContentAsync(string prompt, string? responseSchemaJson = null)
        {
            var model = Environment.GetEnvironmentVariable("GEMINI_MODEL")?.Trim()
                        ?? "gemini-2.5-flash";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";

            object? schemaObj = null;
            if (!string.IsNullOrEmpty(responseSchemaJson))
                schemaObj = JsonSerializer.Deserialize<object>(responseSchemaJson);

            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { responseMimeType = "application/json", responseSchema = schemaObj }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var requestBody = JsonSerializer.Serialize(payload, jsonOptions);
            var response = await PostWithRetryAsync(url, requestBody);
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
                candidates[0].TryGetProperty("content", out var cc) &&
                cc.TryGetProperty("parts", out var pp) &&
                pp.GetArrayLength() > 0 &&
                pp[0].TryGetProperty("text", out var textEl))
                return textEl.GetString() ?? string.Empty;

            throw new InvalidOperationException($"Cannot parse Gemini response: {responseBody}");
        }

        // ─── PostWithRetry ────────────────────────────────────────────────────
        private async Task<HttpResponseMessage> PostWithRetryAsync(string url, string requestBody, int maxRetries = 3, int delayMs = 1500)
        {
            HttpResponseMessage? response = null;
            for (int i = 0; i < maxRetries; i++)
            {
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode) return response;
                if ((response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                     response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                     (int)response.StatusCode == 429 || (int)response.StatusCode == 503) &&
                    i < maxRetries - 1)
                {
                    await Task.Delay(delayMs * (i + 1));
                    continue;
                }
                break;
            }
            return response ?? new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
        }

        // ─── Embeddings ───────────────────────────────────────────────────────
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={_apiKey}";
            var requestBody = new
            {
                model = "models/gemini-embedding-001",
                content = new { parts = new[] { new { text } } }
            };
            var response = await PostWithRetryAsync(url, JsonSerializer.Serialize(requestBody));
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini Embedding Error: {response.StatusCode} - {errorBody}");
            }
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseString);
            return result.GetProperty("embedding").GetProperty("values").EnumerateArray()
                .Select(x => x.GetSingle()).Take(768).ToArray();
        }

        public async Task<List<float[]>> GetEmbeddingsAsync(List<string> texts)
        {
            var results = new List<float[]>();
            foreach (var t in texts) results.Add(await GetEmbeddingAsync(t));
            return results;
        }

        // ─── GenerateAnswer (blocking) ─────────────────────────────────────────
        public async Task<string> GenerateAnswerAsync(string prompt, string? modelName = null)
        {
            modelName = string.IsNullOrWhiteSpace(modelName) ? _defaultModel : modelName;
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKey}";
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };

            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var result = JsonDocument.Parse(responseString);
                    var cands = result.RootElement.GetProperty("candidates");
                    if (cands.GetArrayLength() == 0) return string.Empty;
                    var parts = cands[0].GetProperty("content").GetProperty("parts");
                    if (parts.GetArrayLength() == 0) return string.Empty;
                    return parts[0].GetProperty("text").GetString() ?? string.Empty;
                }
                var statusCode = (int)response.StatusCode;
                if ((statusCode == 429 || statusCode == 503) && attempt < maxRetries)
                {
                    await Task.Delay(attempt * 6000);
                    continue;
                }
                if (statusCode == 429) return "⚠️ Trợ lý AI đang quá tải, vui lòng thử lại sau vài giây.";
                if (statusCode == 503) return "⚠️ Dịch vụ AI tạm thời không khả dụng.";
                var errorBody = await response.Content.ReadAsStringAsync();
                return $"Lỗi khi gọi AI: {response.StatusCode} - {errorBody}";
            }
            return "⚠️ Không thể kết nối đến dịch vụ AI sau nhiều lần thử.";
        }

        // ─── GenerateStreamingAnswer (SSE) ─────────────────────────────────────
        /// <inheritdoc/>
        public async IAsyncEnumerable<string> GenerateStreamingAnswerAsync(
            string prompt,
            string? modelName = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            modelName = string.IsNullOrWhiteSpace(modelName) ? _defaultModel : modelName;
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:streamGenerateContent?alt=sse&key={_apiKey}";
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = jsonContent };
                response = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (OperationCanceledException) { yield break; }

            if (!response.IsSuccessStatusCode)
            {
                var sc = (int)response.StatusCode;
                if (sc == 429) yield return "⚠️ Trợ lý AI đang quá tải, vui lòng thử lại sau vài giây.";
                else if (sc == 503) yield return "⚠️ Dịch vụ AI tạm thời không khả dụng.";
                else yield return $"⚠️ Lỗi kết nối AI ({sc}).";
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                string? line;
                try { line = await reader.ReadLineAsync(); }
                catch (OperationCanceledException) { yield break; }

                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;
                var jsonStr = line.Substring("data: ".Length).Trim();
                if (jsonStr == "[DONE]") break;

                string? chunkText = null;
                try
                {
                    using var doc = JsonDocument.Parse(jsonStr);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("candidates", out var cands) &&
                        cands.GetArrayLength() > 0 &&
                        cands[0].TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0 &&
                        parts[0].TryGetProperty("text", out var tp))
                        chunkText = tp.GetString();
                }
                catch { /* bỏ qua chunk lỗi parse */ }

                if (!string.IsNullOrEmpty(chunkText))
                    yield return chunkText;
            }
        }

        // ─── GetAvailableModels ───────────────────────────────────────────────
        public async Task<List<GeminiModelInfo>> GetAvailableModelsAsync()
        {
            var defaultList = new List<GeminiModelInfo>
            {
                new("gemini-2.5-flash", "Gemini 2.5 Flash"),
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
                using var doc = JsonDocument.Parse(json);
                var models = new List<GeminiModelInfo>();
                foreach (var model in doc.RootElement.GetProperty("models").EnumerateArray())
                {
                    var name = model.GetProperty("name").GetString() ?? string.Empty;
                    var id = name.Replace("models/", "");
                    bool supportsGenerate = false;
                    if (model.TryGetProperty("supportedGenerationMethods", out var methods))
                        foreach (var m in methods.EnumerateArray())
                            if (m.GetString() == "generateContent") { supportsGenerate = true; break; }
                    if (!supportsGenerate) continue;
                    var displayName = model.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? id : id;
                    models.Add(new GeminiModelInfo(id, displayName));
                }
                return models.Count > 0 ? models : defaultList;
            }
            catch { return defaultList; }
        }
    }
}
