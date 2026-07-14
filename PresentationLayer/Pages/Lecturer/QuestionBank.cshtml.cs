using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using DataAccessLayer.Entities;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    public class QuestionBankModel : PageModel
    {
        private readonly IQuestionBankService _questionService;
        private readonly IUserService _userService;
        private readonly IAIQuizGeneratorService _aiQuizService;

        public QuestionBankModel(IQuestionBankService questionService, IUserService userService, IAIQuizGeneratorService aiQuizService)
        {
            _questionService = questionService;
            _userService = userService;
            _aiQuizService = aiQuizService;
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
        public CreateQuestionDto NewQuestion { get; set; } = new CreateQuestionDto();
        public int EditQuestionId { get; set; }
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

        public async Task<IActionResult> OnPostAddAsync(CreateQuestionDto NewQuestion)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Keys
                    .Where(k => ModelState[k].Errors.Count > 0)
                    .Select(k => k + ": " + string.Join(", ", ModelState[k].Errors.Select(e => e.ErrorMessage))));
                StatusMessage = "Error: Dữ liệu câu hỏi không hợp lệ. Chi tiết: " + errors;
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

        public async Task<IActionResult> OnPostEditAsync(int EditQuestionId, CreateQuestionDto EditQuestion)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Keys
                    .Where(k => ModelState[k].Errors.Count > 0)
                    .Select(k => k + ": " + string.Join(", ", ModelState[k].Errors.Select(e => e.ErrorMessage))));
                StatusMessage = "Error: Dữ liệu câu hỏi cập nhật không hợp lệ. Chi tiết: " + errors;
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

        public async Task<IActionResult> OnGetGenerateSingleQuestionAsync(int subjectId, string topic)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return new JsonResult(new { success = false, message = "Unauthorized" });

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null) return new JsonResult(new { success = false, message = "Lecturer not found" });

            try
            {
                var request = new AIGenerateRequestDto
                {
                    SubjectId = subjectId,
                    Topic = topic,
                    Count = 1,
                    Difficulty = "Medium",
                    QuestionType = "All"
                };

                var result = await _aiQuizService.GenerateQuestionsAsync(request, user.Id);
                var list = result != null ? result.ToList() : new List<AIGenerateResultDto>();
                if (list.Count > 0)
                {
                    var q = list[0];
                    return new JsonResult(new { success = true, question = q });
                }
                return new JsonResult(new { success = false, message = "Không thể sinh câu hỏi từ AI." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
        public async Task<IActionResult> OnGetDeletedQuestionsAsync(int page = 1)
        {
            try
            {
                var result = await _questionService.GetDeletedPagedQuestionsAsync(page, PageSize);
                return new JsonResult(new {
                    success = true,
                    items = result.Items,
                    totalCount = result.TotalCount,
                    pageSize = PageSize,
                    currentPage = page,
                    totalPages = (int)Math.Ceiling((double)result.TotalCount / PageSize)
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostRestoreAsync(int id)
        {
            var success = await _questionService.RestoreQuestionAsync(id);
            if (success)
            {
                StatusMessage = "Thành công: Khôi phục câu hỏi thành công.";
            }
            else
            {
                StatusMessage = "Error: Khôi phục câu hỏi thất bại.";
            }
            return RedirectToPage(new { SubjectId, Difficulty, Type, Search, CurrentPage });
        }

        public async Task<IActionResult> OnPostHardDeleteAsync(int id)
        {
            var success = await _questionService.HardDeleteQuestionAsync(id);
            if (success)
            {
                StatusMessage = "Thành công: Xóa vĩnh viễn câu hỏi thành công.";
            }
            else
            {
                StatusMessage = "Error: Xóa vĩnh viễn câu hỏi thất bại.";
            }
            return RedirectToPage(new { SubjectId, Difficulty, Type, Search, CurrentPage });
        }
    }
}
