using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository truy vấn Bài kiểm tra và câu hỏi liên kết.
    /// </summary>
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

        public async Task<IEnumerable<Quiz>> GetQuizzesForStudentAsync(int studentId)
        {
            // Tạm thời lấy tất cả bài thi (Student có thể thấy tất cả bài thi hoặc được cung cấp AccessCode)
            // Nếu sau này có bảng trung gian Enrollment/Subscription thì thêm điều kiện Join vào đây.
            return await _context.Quizzes
                .Include(q => q.Subject)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Quiz quiz)
        {
            await _context.Quizzes.AddAsync(quiz);
            await _context.SaveChangesAsync();
        }

        public async Task AddQuestionsAsync(IEnumerable<QuizQuestion> questions)
        {
            await _context.QuizQuestions.AddRangeAsync(questions);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<QuizQuestion>> GetQuizQuestionsAsync(int quizId)
        {
            return await _context.QuizQuestions
                .Include(q => q.QuestionBank)
                .Where(q => q.QuizId == quizId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();
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
