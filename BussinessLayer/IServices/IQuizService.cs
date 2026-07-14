using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Quản lý Bài thi. Bao gồm nghiệp vụ cho Giảng viên (tạo/sửa/xóa/thống kê)
    /// và Sinh viên (danh sách, làm bài, lưu tiến trình, nộp bài, xem kết quả).
    /// </summary>
    public interface IQuizService
    {
        // === LECTURER ACTIONS ===
        Task<int> CreateQuizAsync(int lecturerId, CreateQuizDto dto);
        Task<QuizStatisticsDto> GetQuizStatisticsAsync(int quizId, int lecturerId, bool isAdmin = false);
        Task<List<QuizSummaryDto>> GetQuizzesBySubjectAsync(int subjectId);
        Task<UpdateQuizDto> GetQuizForUpdateAsync(int lecturerId, int quizId, bool isAdmin = false);
        Task<List<QuizQuestionDetailDto>> GetQuizQuestionsDetailAsync(int quizId, int lecturerId, bool isAdmin = false);
        Task UpdateQuizAsync(int lecturerId, int quizId, UpdateQuizDto dto, bool isAdmin = false);
        Task DeleteQuizAsync(int lecturerId, int quizId, bool isAdmin = false);

        // === STUDENT ACTIONS ===
        Task<List<StudentQuizDto>> GetStudentQuizzesAsync(int studentId);
        Task<(bool Success, string Message)> CheckQuizAccessCodeAsync(int studentId, int quizId, string? accessCode);
        Task<TakeQuizDto?> GetInProgressAttemptSummaryAsync(int studentId, int attemptId);
        Task<QuizDetailDto> GetQuizDetailAsync(int quizId, int studentId);
        Task<TakeQuizDto> StartQuizAsync(int studentId, int quizId, string? accessCode, bool createNew = false);
        Task SaveQuizProgressAsync(int studentId, SubmitQuizDto dto);
        Task<QuizResultDto> SubmitQuizAsync(int studentId, SubmitQuizDto dto);
        Task<QuizResultDto> GetAttemptResultAsync(int attemptId, int studentId);
    }
}
