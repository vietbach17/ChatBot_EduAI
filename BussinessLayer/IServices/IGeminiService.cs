using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BussinessLayer.IServices
{
    public record GeminiModelInfo(string Id, string DisplayName);

    public interface IGeminiService
    {
        Task<float[]> GetEmbeddingAsync(string text);
        Task<List<float[]>> GetEmbeddingsAsync(List<string> texts);
        Task<string> GenerateAnswerAsync(string prompt, string? modelName = null);
        Task<List<GeminiModelInfo>> GetAvailableModelsAsync();
        Task<string> GenerateJsonContentAsync(string prompt, string? responseSchemaJson = null);

        /// <summary>
        /// Stream từng chunk text từ Gemini API qua SSE (streamGenerateContent).
        /// </summary>
        IAsyncEnumerable<string> GenerateStreamingAnswerAsync(string prompt, string? modelName = null, CancellationToken cancellationToken = default);
    }
}

