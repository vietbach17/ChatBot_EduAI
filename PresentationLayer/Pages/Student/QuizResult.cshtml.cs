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
    public class QuizResultModel : PageModel
    {
        private readonly IQuizService _quizService;

        public QuizResultModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public QuizResultDto Result { get; set; } = default!;
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

                Result = await _quizService.GetAttemptResultAsync(id, studentId);
                
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }
    }
}
