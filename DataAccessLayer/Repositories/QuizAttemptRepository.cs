using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>Repository truy vấn Lượt làm bài thi và câu trả lời của sinh viên từ PostgreSQL.</summary>
    public class QuizAttemptRepository : IQuizAttemptRepository
    {
        private readonly ApplicationDbContext _context;

        public QuizAttemptRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<QuizAttempt> CreateAttemptAsync(QuizAttempt attempt)
        {
            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();
            return attempt;
        }

        public async Task<QuizAttempt?> GetAttemptByIdAsync(int id)
        {
            return await _context.QuizAttempts
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<QuizAttempt?> GetAttemptWithAnswersAsync(int id)
        {
            return await _context.QuizAttempts
                .Include(q => q.Quiz)
                .Include(q => q.Answers)
                .ThenInclude(a => a.QuestionBank)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<IEnumerable<QuizAttempt>> GetAttemptsByStudentAsync(int studentId, int quizId)
        {
            return await _context.QuizAttempts
                .Where(q => q.StudentId == studentId && q.QuizId == quizId)
                .OrderByDescending(q => q.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<QuizAttempt>> GetAttemptsByStudentAsync(int studentId)
        {
            return await _context.QuizAttempts
                .Where(q => q.StudentId == studentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<QuizAttempt>> GetAllAttemptsForQuizAsync(int quizId)
        {
            return await _context.QuizAttempts
                .Include(q => q.Student)
                .Where(q => q.QuizId == quizId)
                .OrderByDescending(q => q.Score)
                .ToListAsync();
        }

        public async Task UpdateAttemptAsync(QuizAttempt attempt)
        {
            _context.QuizAttempts.Update(attempt);
            await _context.SaveChangesAsync();
        }

        public async Task AddAnswersAsync(IEnumerable<QuizAnswer> answers)
        {
            _context.QuizAnswers.AddRange(answers);
            await _context.SaveChangesAsync();
        }
    }
}
