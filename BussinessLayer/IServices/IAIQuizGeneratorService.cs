using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Sinh câu hỏi trắc nghiệm tự động bằng AI.
    /// </summary>
    public interface IAIQuizGeneratorService
    {
        Task<IEnumerable<AIGenerateResultDto>> GenerateQuestionsAsync(AIGenerateRequestDto request, int lecturerId);
        Task<IEnumerable<AIGenerationLogDto>> GetGenerationLogsAsync(int lecturerId);
        Task<IEnumerable<AIGenerationLogDto>> GetRecentGenerationLogsAsync(int take);
    }
}
