using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Quản lý Ngân hàng Câu hỏi.
    /// </summary>
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
        Task<bool> UpdateQuestionAsync(int id, CreateQuestionDto updateDto, int userId);
        Task<bool> DeleteQuestionAsync(int id, int userId);
        Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync();
        Task<Dictionary<string, int>> GetQuestionStatisticsAsync(int subjectId);

        // Trash bin service methods
        Task<(IEnumerable<QuestionBankDto> Items, int TotalCount)> GetDeletedPagedQuestionsAsync(int page, int pageSize);
        Task<bool> RestoreQuestionAsync(int id, int userId);
        Task<bool> HardDeleteQuestionAsync(int id, int userId);
    }
}
