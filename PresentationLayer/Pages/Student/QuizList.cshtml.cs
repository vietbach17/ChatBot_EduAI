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
            int studentId = 0;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            {
                studentId = uid;
            }

            if (studentId == 0) return RedirectToPage("/Auth/Login");

            Quizzes = await _quizService.GetStudentQuizzesAsync(studentId);
            return Page();
        }
    }
}
