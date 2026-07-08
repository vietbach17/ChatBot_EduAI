using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using DataAccessLayer.Entities;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    public class QuestionBankModel : PageModel
    {
        private readonly IQuestionBankService _questionService;
        private readonly IUserService _userService;

        public QuestionBankModel(IQuestionBankService questionService, IUserService userService)
        {
            _questionService = questionService;
            _userService = userService;
        }

        // Filters and paging
        [BindProperty(SupportsGet = true)]
        public int SubjectId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Difficulty { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Type { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        // Data lists
        public IEnumerable<QuestionBankDto> Questions { get; set; } = new List<QuestionBankDto>();
        public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();

        // Forms and actions
        [BindProperty]
        public CreateQuestionDto NewQuestion { get; set; } = new CreateQuestionDto();

        [BindProperty]
        public int EditQuestionId { get; set; }

        [BindProperty]
        public CreateQuestionDto EditQuestion { get; set; } = new CreateQuestionDto();

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Subjects = await _questionService.GetAllSubjectsAsync();
            
            var result = await _questionService.GetPagedQuestionsAsync(
                SubjectId, Difficulty, Type, Search, CurrentPage, PageSize);
            
            Questions = result.Items;
            TotalCount = result.TotalCount;

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: Dữ liệu câu hỏi không hợp lệ.";
                return RedirectToPage(new { SubjectId, Difficulty, Type, Search, CurrentPage });
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Challenge();

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null) return Challenge();

            // Set default JSON structure for True/False if not Multiple Choice
            if (NewQuestion.QuestionType == "TrueFalse")
            {
                NewQuestion.OptionsJson = null;
            }

            var success = await _questionService.AddQuestionAsync(NewQuestion, user.Id);
            if (success)
            {
                StatusMessage = "Thành công: Thêm câu hỏi mới vào ngân hàng thành công.";
            }
            else
            {
                StatusMessage = "Error: Thêm câu hỏi thất bại.";
            }

            return RedirectToPage(new { SubjectId, Difficulty, Type, Search, CurrentPage });
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: Dữ liệu câu hỏi cập nhật không hợp lệ.";
                return RedirectToPage(new { SubjectId, Difficulty, Type, Search, CurrentPage });
            }

            if (EditQuestion.QuestionType == "TrueFalse")
            {
                EditQuestion.OptionsJson = null;
            }

            var success = await _questionService.UpdateQuestionAsync(EditQuestionId, EditQuestion);
            if (success)
            {
                StatusMessage = "Thành công: Cập nhật câu hỏi thành công.";
            }
            else
            {
                StatusMessage = "Error: Cập nhật câu hỏi thất bại.";
            }

            return RedirectToPage(new { SubjectId, Difficulty, Type, Search, CurrentPage });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var success = await _questionService.DeleteQuestionAsync(id);
            if (success)
            {
                StatusMessage = "Thành công: Xóa câu hỏi thành công.";
            }
            else
            {
                StatusMessage = "Error: Xóa câu hỏi thất bại hoặc câu hỏi không tồn tại.";
            }

            return RedirectToPage(new { SubjectId, Difficulty, Type, Search, CurrentPage });
        }
    }
}
