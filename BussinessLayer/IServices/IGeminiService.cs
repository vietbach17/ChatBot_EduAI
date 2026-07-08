using System.Collections.Generic;
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
    }
}
