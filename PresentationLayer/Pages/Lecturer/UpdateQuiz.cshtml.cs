using System;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.SignalR;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer,Admin")]
    /// <summary>PageModel trang Sửa bài thi (Giảng viên/Admin). Nạp và cập nhật cấu hình bài thi hiện có.</summary>
    public class UpdateQuizModel : PageModel
    {
        private readonly IQuizService _quizService;
        private readonly ISubjectService _subjectService;
        private readonly IQuizActivityLogService _activityLogService;
        private readonly IHubContext<SignalRHub> _hubContext;

        public UpdateQuizModel(IQuizService quizService, ISubjectService subjectService, IQuizActivityLogService activityLogService, IHubContext<SignalRHub> hubContext)
        {
            _quizService = quizService;
            _subjectService = subjectService;
            _activityLogService = activityLogService;
            _hubContext = hubContext;
        }

        [BindProperty(SupportsGet = true)]
        public int QuizId { get; set; }

        [BindProperty]
        public int SubjectId { get; set; }

        public string SubjectTitle { get; set; } = string.Empty;

        [BindProperty]
        public UpdateQuizDto QuizInput { get; set; } = new UpdateQuizDto();

        public List<QuizQuestionDetailDto> QuizQuestions { get; set; } = new List<QuizQuestionDetailDto>();
        public bool IsAdmin { get; set; } = false;

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

            IsAdmin = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value == "Admin";

            try
            {
                var quizStats = await _quizService.GetQuizStatisticsAsync(QuizId, lecturerId, IsAdmin);
                SubjectTitle = quizStats.QuizTitle; // Just to display
                SubjectId = quizStats.SubjectId;

                QuizInput = await _quizService.GetQuizForUpdateAsync(lecturerId, QuizId, IsAdmin);
                QuizQuestions = await _quizService.GetQuizQuestionsDetailAsync(QuizId, lecturerId, IsAdmin);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsAdmin = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value == "Admin";
            if (IsAdmin)
            {
                return Forbid(); // Admin is read-only
            }
            if (QuizId <= 0) return RedirectToPage("/Lecturer/MySubjects");

            int lecturerId = 1;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            {
                lecturerId = uid;
            }

            try
            {
                await _quizService.UpdateQuizAsync(lecturerId, QuizId, QuizInput, IsAdmin);
                await _activityLogService.LogActivityAsync(SubjectId, QuizId, QuizInput.Title, lecturerId, "Updated");
                await _hubContext.Clients.All.SendAsync("CourseChanged");
                TempData["SuccessMessage"] = "Cập nhật bài thi thành công!";
                return RedirectToPage("/Lecturer/ManageSubject", new { id = SubjectId });
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }
    }
}
