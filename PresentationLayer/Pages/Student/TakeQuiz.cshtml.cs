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
    [IgnoreAntiforgeryToken]
    /// <summary>PageModel trang Làm bài thi (Sinh viên). Bắt đầu lượt làm, lưu tiến trình tự động và nộp bài.</summary>
    public class TakeQuizModel : PageModel
    {
        private readonly IQuizService _quizService;

        public TakeQuizModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public TakeQuizDto QuizAttempt { get; set; } = default!;
        public string ErrorMessage { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? AccessCode { get; set; }

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

                // Sử dụng AccessCode từ Query String, không tạo mới attempt nếu gọi GET
                QuizAttempt = await _quizService.StartQuizAsync(studentId, id, AccessCode, createNew: false);
                
                return Page();
            }
            catch (Exception ex)
            {
                if (ex.Message == "NO_IN_PROGRESS")
                {
                    return RedirectToPage("/Student/QuizList");
                }
                ErrorMessage = ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostStartAsync(int id, [FromForm] string? accessCode)
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

                // Yêu cầu tạo attempt mới
                await _quizService.StartQuizAsync(studentId, id, accessCode, createNew: true);
                
                // Redirect về GET để tránh form resubmission
                return RedirectToPage("/Student/TakeQuiz", new { id = id, AccessCode = accessCode });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSubmitAsync([FromBody] SubmitQuizDto dto)
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

                var result = await _quizService.SubmitQuizAsync(studentId, dto);
                
                return new JsonResult(new { success = true, score = result.Score, attemptId = result.AttemptId });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostSaveProgressAsync([FromBody] SubmitQuizDto dto)
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

                await _quizService.SaveQuizProgressAsync(studentId, dto);
                
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}
