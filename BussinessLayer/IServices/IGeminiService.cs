using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BussinessLayer.IServices
{
    /// <summary>Thông tin một model Gemini: mã định danh và tên hiển thị.</summary>
    public record GeminiModelInfo(string Id, string DisplayName);

    /// <summary>
    /// Số token thực tế của một lượt gọi Gemini (từ usageMetadata trong response)
    /// kèm model đã trả lời thành công (có thể khác model yêu cầu do cơ chế fallback).
    /// </summary>
    public record GeminiTokenUsage(string Model, int PromptTokens, int OutputTokens)
    {
        public int TotalTokens => PromptTokens + OutputTokens;
    }

    /// <summary>

    /// Interface dich vu Google Gemini AI. Dinh nghia cac phuong thuc sinh cau tra loi, streaming va tao vector embedding.

    /// </summary>

    public interface IGeminiService
    {
        Task<float[]> GetEmbeddingAsync(string text);
        Task<List<float[]>> GetEmbeddingsAsync(List<string> texts);
        /// <param name="onUsage">Callback nhận số token thực tế (usageMetadata) khi gọi thành công.</param>
        Task<string> GenerateAnswerAsync(string prompt, string? modelName = null, int maxRetries = 4, Action<GeminiTokenUsage>? onUsage = null);
        Task<List<GeminiModelInfo>> GetAvailableModelsAsync();
        Task<string> GenerateJsonContentAsync(string prompt, string? responseSchemaJson = null);

        /// <summary>
        /// Stream từng chunk text từ Gemini API qua SSE (streamGenerateContent).
        /// </summary>
        /// <param name="onUsage">Callback nhận số token thực tế (usageMetadata ở chunk SSE cuối) sau khi stream xong.</param>
        IAsyncEnumerable<string> GenerateStreamingAnswerAsync(string prompt, string? modelName = null, CancellationToken cancellationToken = default, Action<GeminiTokenUsage>? onUsage = null);
    }
}

