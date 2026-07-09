using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class QuizRepository : IQuizRepository
    {
        private readonly ApplicationDbContext _context;

        public QuizRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Quiz?> GetByIdAsync(int id)
        {
            return await _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Lecturer)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<IEnumerable<Quiz>> GetBySubjectOrLecturerAsync(int? subjectId, int? lecturerId)
        {
            var query = _context.Quizzes
                .Include(q => q.Subject)
                .Include(q => q.Lecturer)
                .AsQueryable();

            if (subjectId.HasValue && subjectId.Value > 0)
            {
                query = query.Where(q => q.SubjectId == subjectId.Value);
            }

            if (lecturerId.HasValue && lecturerId.Value > 0)
            {
                query = query.Where(q => q.LecturerId == lecturerId.Value);
            }

            return await query.OrderByDescending(q => q.CreatedAt).ToListAsync();
        }

        public async Task AddAsync(Quiz quiz)
        {
            await _context.Quizzes.AddAsync(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Quiz quiz)
        {
            _context.Quizzes.Update(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var quiz = await _context.Quizzes.FindAsync(id);
            if (quiz != null)
            {
                _context.Quizzes.Remove(quiz);
                await _context.SaveChangesAsync();
            }
        }
    }
}
