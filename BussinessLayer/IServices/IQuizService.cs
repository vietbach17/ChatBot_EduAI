using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    public interface IQuizService
    {
        // === LECTURER ACTIONS ===
        Task<int> CreateQuizAsync(int lecturerId, CreateQuizDto dto);
        Task<QuizStatisticsDto> GetQuizStatisticsAsync(int quizId, int lecturerId);
        Task<List<QuizSummaryDto>> GetQuizzesBySubjectAsync(int subjectId);
        Task<UpdateQuizDto> GetQuizForUpdateAsync(int lecturerId, int quizId);
        Task UpdateQuizAsync(int lecturerId, int quizId, UpdateQuizDto dto);
        Task DeleteQuizAsync(int lecturerId, int quizId);

        // === STUDENT ACTIONS ===
        Task<List<StudentQuizDto>> GetStudentQuizzesAsync(int studentId);
        Task<QuizDetailDto> GetQuizDetailAsync(int quizId, int studentId);
        Task<TakeQuizDto> StartQuizAsync(int studentId, int quizId, string? accessCode);
        Task<QuizResultDto> SubmitQuizAsync(int studentId, SubmitQuizDto dto);
        Task<QuizResultDto> GetAttemptResultAsync(int attemptId, int studentId);
    }
}
