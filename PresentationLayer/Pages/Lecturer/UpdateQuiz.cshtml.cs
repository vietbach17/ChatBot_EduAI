using System;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    public class UpdateQuizModel : PageModel
    {
        private readonly IQuizService _quizService;
        private readonly ISubjectService _subjectService;

        public UpdateQuizModel(IQuizService quizService, ISubjectService subjectService)
        {
            _quizService = quizService;
            _subjectService = subjectService;
        }

        [BindProperty(SupportsGet = true)]
        public int QuizId { get; set; }

        public int SubjectId { get; set; }
        
        public string SubjectTitle { get; set; } = string.Empty;

        [BindProperty]
        public UpdateQuizDto QuizInput { get; set; } = new UpdateQuizDto();

        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (QuizId <= 0) return RedirectToPage("/Lecturer/MySubjects");

            int lecturerId = 1;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            {
                lecturerId = uid;
            }

            try
            {
                var quizStats = await _quizService.GetQuizStatisticsAsync(QuizId, lecturerId);
                SubjectTitle = quizStats.QuizTitle; // Just to display
                
                // Fetch details for update
                QuizInput = await _quizService.GetQuizForUpdateAsync(lecturerId, QuizId);
            }
            catch(Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (QuizId <= 0) return RedirectToPage("/Lecturer/MySubjects");

            int lecturerId = 1;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            {
                lecturerId = uid;
            }

            try
            {
                await _quizService.UpdateQuizAsync(lecturerId, QuizId, QuizInput);
                TempData["SuccessMessage"] = "Cập nhật bài thi thành công!";
                return RedirectToPage(new { QuizId = QuizId }); // Reload with success message
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }
    }
}
