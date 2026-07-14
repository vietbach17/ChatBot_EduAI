using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    /// <summary>Giao diện Repository truy vấn Lượt làm bài thi và câu trả lời của sinh viên.</summary>
    public interface IQuizAttemptRepository
    {
        Task<QuizAttempt> CreateAttemptAsync(QuizAttempt attempt);
        Task<QuizAttempt?> GetAttemptByIdAsync(int id);
        Task<QuizAttempt?> GetAttemptWithAnswersAsync(int id);
        Task<IEnumerable<QuizAttempt>> GetAttemptsByStudentAsync(int studentId, int quizId);
        Task<IEnumerable<QuizAttempt>> GetAttemptsByStudentAsync(int studentId);
        Task<IEnumerable<QuizAttempt>> GetAllAttemptsForQuizAsync(int quizId);
        Task UpdateAttemptAsync(QuizAttempt attempt);
        Task AddAnswersAsync(IEnumerable<QuizAnswer> answers);
    }
}
