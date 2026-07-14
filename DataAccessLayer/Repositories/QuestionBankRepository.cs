using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository truy vấn Ngân hàng Câu hỏi, hỗ trợ lọc và phân trang.
    /// </summary>
    public class QuestionBankRepository : IQuestionBankRepository
    {
        private readonly ApplicationDbContext _context;

        public QuestionBankRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionBank?> GetByIdAsync(int id)
        {
            return await _context.QuestionBanks
                .Include(q => q.Subject)
                .Include(q => q.Lecturer)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<IEnumerable<QuestionBank>> GetPagedAsync(
            int subjectId,
            string? difficulty,
            string? type,
            string? search,
            int page,
            int pageSize)
        {
            var query = _context.QuestionBanks
                .Include(q => q.Subject)
                .Include(q => q.Lecturer)
                .Where(q => !q.IsDeleted)
                .AsQueryable();

            if (subjectId > 0)
            {
                query = query.Where(q => q.SubjectId == subjectId);
            }

            if (!string.IsNullOrEmpty(difficulty))
            {
                query = query.Where(q => q.Difficulty == difficulty);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(q => q.QuestionType == type);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(q => q.Content.ToLower().Contains(search.ToLower()));
            }

            return await query
                .OrderByDescending(q => q.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task AddAsync(QuestionBank question)
        {
            await _context.QuestionBanks.AddAsync(question);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(QuestionBank question)
        {
            _context.QuestionBanks.Update(question);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var question = await _context.QuestionBanks.FindAsync(id);
            if (question != null)
            {
                question.IsDeleted = true;
                _context.QuestionBanks.Update(question);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> CountAsync(int subjectId, string? difficulty, string? type, string? search)
        {
            var query = _context.QuestionBanks.Where(q => !q.IsDeleted).AsQueryable();

            if (subjectId > 0)
            {
                query = query.Where(q => q.SubjectId == subjectId);
            }

            if (!string.IsNullOrEmpty(difficulty))
            {
                query = query.Where(q => q.Difficulty == difficulty);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(q => q.QuestionType == type);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(q => q.Content.ToLower().Contains(search.ToLower()));
            }

            return await query.CountAsync();
        }

        public async Task<IEnumerable<QuestionBank>> GetDeletedPagedAsync(int page, int pageSize)
        {
            return await _context.QuestionBanks
                .Include(q => q.Subject)
                .Include(q => q.Lecturer)
                .Where(q => q.IsDeleted)
                .OrderByDescending(q => q.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountDeletedAsync()
        {
            return await _context.QuestionBanks
                .Where(q => q.IsDeleted)
                .CountAsync();
        }

        public async Task RestoreAsync(int id)
        {
            var question = await _context.QuestionBanks.FindAsync(id);
            if (question != null)
            {
                question.IsDeleted = false;
                _context.QuestionBanks.Update(question);
                await _context.SaveChangesAsync();
            }
        }

        public async Task HardDeleteAsync(int id)
        {
            var question = await _context.QuestionBanks.FindAsync(id);
            if (question != null)
            {
                _context.QuestionBanks.Remove(question);
                await _context.SaveChangesAsync();
            }
        }
    }
}
