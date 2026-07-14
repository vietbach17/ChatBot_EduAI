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
    [Authorize(Roles = "Lecturer,Admin")]
    /// <summary>PageModel trang Kết quả bài thi (Giảng viên/Admin). Hiển thị thống kê điểm và danh sách lượt làm của sinh viên.</summary>
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

            bool isAdmin = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value == "Admin";

            try
            {
                Statistics = await _quizService.GetQuizStatisticsAsync(QuizId, lecturerId, isAdmin);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            return Page();
        }
    }
}
