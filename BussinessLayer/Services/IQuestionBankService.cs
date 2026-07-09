using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;

namespace BussinessLayer.Services
{
    public interface IQuestionBankService
    {
        Task<QuestionBankDto?> GetQuestionByIdAsync(int id);
        Task<(IEnumerable<QuestionBankDto> Items, int TotalCount)> GetPagedQuestionsAsync(
            int subjectId,
            string? difficulty,
            string? type,
            string? search,
            int page,
            int pageSize);
        Task<bool> AddQuestionAsync(CreateQuestionDto createDto, int lecturerId);
        Task<bool> UpdateQuestionAsync(int id, CreateQuestionDto updateDto);
        Task<bool> DeleteQuestionAsync(int id);
        Task<IEnumerable<Subject>> GetAllSubjectsAsync();
        Task<Dictionary<string, int>> GetQuestionStatisticsAsync(int subjectId);

        // Trash bin service methods
        Task<(IEnumerable<QuestionBankDto> Items, int TotalCount)> GetDeletedPagedQuestionsAsync(int page, int pageSize);
        Task<bool> RestoreQuestionAsync(int id);
        Task<bool> HardDeleteQuestionAsync(int id);
    }
}
