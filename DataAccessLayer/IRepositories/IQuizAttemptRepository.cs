using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    public interface IQuizAttemptRepository
    {
        Task<QuizAttempt> CreateAttemptAsync(QuizAttempt attempt);
        Task<QuizAttempt?> GetAttemptByIdAsync(int id);
        Task<QuizAttempt?> GetAttemptWithAnswersAsync(int id);
        Task<IEnumerable<QuizAttempt>> GetAttemptsByStudentAsync(int studentId, int quizId);
        Task<IEnumerable<QuizAttempt>> GetAllAttemptsForQuizAsync(int quizId);
        Task UpdateAttemptAsync(QuizAttempt attempt);
        Task AddAnswersAsync(IEnumerable<QuizAnswer> answers);
    }
}
