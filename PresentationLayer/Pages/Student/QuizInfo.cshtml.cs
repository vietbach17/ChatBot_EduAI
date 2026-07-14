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
    /// <summary>PageModel trang Thông tin bài thi (Sinh viên). Hiển thị chi tiết bài thi và lịch sử lượt làm trước khi vào thi.</summary>
    public class QuizInfoModel : PageModel
    {
        private readonly IQuizService _quizService;

        public QuizInfoModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public QuizDetailDto QuizInfo { get; set; } = default!;
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

                QuizInfo = await _quizService.GetQuizDetailAsync(id, studentId);
                
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
