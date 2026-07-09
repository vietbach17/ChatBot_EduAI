using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

using DataAccessLayer.Entities;

namespace BussinessLayer.Services
{
    public interface IAIQuizGeneratorService
    {
        Task<IEnumerable<AIGenerateResultDto>> GenerateQuestionsAsync(AIGenerateRequestDto request, int lecturerId);
        Task<IEnumerable<AIGenerationLog>> GetGenerationLogsAsync(int lecturerId);
    }
}
