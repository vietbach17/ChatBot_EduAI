using System;
using System.Collections.Generic;
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
    /// <summary>PageModel trang Danh sách bài thi (Sinh viên). Liệt kê bài thi, trạng thái làm bài và kiểm tra mật khẩu vào thi.</summary>
    public class QuizListModel : PageModel
    {
        private readonly IQuizService _quizService;

        public QuizListModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public List<StudentQuizDto> Quizzes { get; set; } = new List<StudentQuizDto>();

        public async Task<IActionResult> OnGetAsync()
        {
            var studentId = GetStudentId();
            if (studentId == 0) return RedirectToPage("/Auth/Login");

            Quizzes = await _quizService.GetStudentQuizzesAsync(studentId);
            return Page();
        }

        public async Task<IActionResult> OnGetCheckPasswordAsync(int quizId, string accessCode)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return new JsonResult(new { success = false, message = "Phiên đăng nhập hết hạn." });

            var (success, message) = await _quizService.CheckQuizAccessCodeAsync(studentId, quizId, accessCode);
            if (!success) return new JsonResult(new { success = false, message });
            return new JsonResult(new { success = true });
        }

        private int GetStudentId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid) ? uid : 0;
        }
    }
}
