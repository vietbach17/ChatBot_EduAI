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
    public class TakeQuizModel : PageModel
    {
        private readonly IQuizService _quizService;

        public TakeQuizModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public TakeQuizDto QuizAttempt { get; set; } = default!;
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

                // Giả định không dùng password/access code lúc này, hoặc đã check từ trang detail
                QuizAttempt = await _quizService.StartQuizAsync(studentId, id, null);
                
                return Page();
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
    }
}
