using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Giao diện Repository truy vấn Bài kiểm tra (Quiz).
    /// </summary>
    public interface IQuizRepository
    {
        Task<Quiz?> GetByIdAsync(int id);
        Task<IEnumerable<Quiz>> GetBySubjectOrLecturerAsync(int? subjectId, int? lecturerId);
        Task<IEnumerable<Quiz>> GetQuizzesForStudentAsync(int studentId);
        Task<IEnumerable<QuizQuestion>> GetQuizQuestionsAsync(int quizId);
        Task AddAsync(Quiz quiz);
        Task AddQuestionsAsync(IEnumerable<QuizQuestion> questions);
        Task UpdateAsync(Quiz quiz);
        Task DeleteAsync(int id);
        Task<Quiz?> GetByIdIncludeDeletedAsync(int id);
    }
}
