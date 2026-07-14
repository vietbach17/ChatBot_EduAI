using System;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Student
{
    [Authorize(Roles = "Student")]
    /// <summary>PageModel trang Tổng kết trước khi nộp bài (Sinh viên). Hiển thị số câu đã/chưa trả lời và xác nhận nộp bài.</summary>
    public class QuizSummaryModel : PageModel
    {
        private readonly IQuizService _quizService;

        public QuizSummaryModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public TakeQuizDto QuizAttempt { get; set; } = default!;
        public int AnsweredCount { get; set; }
        public int UnansweredCount { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                int studentId = 0;
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
                {
                    studentId = uid;
                }

                if (studentId == 0) return RedirectToPage("/Auth/Login");

                var summary = await _quizService.GetInProgressAttemptSummaryAsync(studentId, id);
                if (summary == null)
                {
                    return RedirectToPage("/Student/QuizList");
                }

                QuizAttempt = summary;

                AnsweredCount = QuizAttempt.Questions.Count(q => !string.IsNullOrEmpty(q.SelectedAnswer));
                UnansweredCount = QuizAttempt.Questions.Count - AnsweredCount;

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSubmitAsync(int id)
        {
            try
            {
                int studentId = 0;
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
                {
                    studentId = uid;
                }

                if (studentId == 0) return Unauthorized();

                // Gửi SubmitQuizDto với Answers = null để backend tự lấy answerRecord.SelectedAnswer chấm điểm
                var dto = new SubmitQuizDto
                {
                    AttemptId = id,
                    Answers = null
                };

                var result = await _quizService.SubmitQuizAsync(studentId, dto);
                
                return RedirectToPage("/Student/QuizResult", new { id = result.AttemptId });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page(); // Ideally should reload with error
            }
        }
    }
}
