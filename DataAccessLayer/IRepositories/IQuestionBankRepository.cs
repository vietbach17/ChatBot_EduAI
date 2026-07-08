using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
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
        Task DeleteAsync(int id);
        Task<int> CountAsync(int subjectId, string? difficulty, string? type, string? search);
    }
}
