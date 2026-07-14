using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Giao diện Repository truy vấn Ngân hàng Câu hỏi.
    /// </summary>
    public interface IQuestionBankRepository
    {
        Task<QuestionBank?> GetByIdAsync(int id);
        Task<IEnumerable<QuestionBank>> GetPagedAsync(
            int subjectId,
            string? difficulty,
            string? type,
            string? search,
            int page,
            int pageSize);
        Task AddAsync(QuestionBank question);
        Task UpdateAsync(QuestionBank question);
        Task DeleteAsync(int id); // Soft-delete
        Task<int> CountAsync(int subjectId, string? difficulty, string? type, string? search);
        
        // Trash bin methods
        Task<IEnumerable<QuestionBank>> GetDeletedPagedAsync(int page, int pageSize);
        Task<int> CountDeletedAsync();
        Task RestoreAsync(int id);
        Task HardDeleteAsync(int id);
    }
}
