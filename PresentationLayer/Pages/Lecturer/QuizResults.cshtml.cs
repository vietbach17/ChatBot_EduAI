using System;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    public class QuizResultsModel : PageModel
    {
        private readonly IQuizService _quizService;

        public QuizResultsModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public QuizStatisticsDto Statistics { get; set; } = new QuizStatisticsDto();
        public string ErrorMessage { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int QuizId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (QuizId <= 0)
                return RedirectToPage("/Lecturer/MySubjects");

            int lecturerId = 1; 
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            {
                lecturerId = uid;
            }

            try
            {
                Statistics = await _quizService.GetQuizStatisticsAsync(QuizId, lecturerId);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }
    }
}
