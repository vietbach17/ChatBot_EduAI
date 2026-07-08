using System;
using System.Collections.Generic;
using System.Linq;
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
    public class AIGenerateQuestionsModel : PageModel
    {
        private readonly IAIQuizGeneratorService _aiGeneratorService;
        private readonly IQuestionBankService _questionService;
        private readonly IUserService _userService;

        public AIGenerateQuestionsModel(
            IAIQuizGeneratorService aiGeneratorService,
            IQuestionBankService questionService,
            IUserService userService)
        {
            _aiGeneratorService = aiGeneratorService;
            _questionService = questionService;
            _userService = userService;
        }

        [BindProperty]
        public AIGenerateRequestDto GenerateRequest { get; set; } = new AIGenerateRequestDto();

        [BindProperty]
        public List<SelectedQuestionViewModel> GeneratedQuestions { get; set; } = new List<SelectedQuestionViewModel>();

        public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();

        [TempData]
        public string? StatusMessage { get; set; }

        public bool HasGenerated { get; set; } = false;

        public async Task<IActionResult> OnGetAsync()
        {
            Subjects = await _questionService.GetAllSubjectsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            Subjects = await _questionService.GetAllSubjectsAsync();

            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: Dữ liệu cấu hình yêu cầu không hợp lệ.";
                return Page();
            }

            try
            {
                var aiResults = await _aiGeneratorService.GenerateQuestionsAsync(GenerateRequest);
                
                GeneratedQuestions = aiResults.Select(r => new SelectedQuestionViewModel
                {
                    IsSelected = true, // Default selected
                    SubjectId = GenerateRequest.SubjectId,
                    Content = r.Content,
                    QuestionType = r.QuestionType,
                    OptionsJson = r.Options != null && r.Options.Any() 
                        ? System.Text.Json.JsonSerializer.Serialize(r.Options) 
                        : null,
                    CorrectAnswer = r.CorrectAnswer,
                    Difficulty = r.Difficulty,
                    Tags = r.Tags
                }).ToList();

                HasGenerated = true;
                StatusMessage = $"Thành công: Đã tạo thành công {GeneratedQuestions.Count} câu hỏi từ AI. Vui lòng xem lại bên dưới.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: Lỗi tạo câu hỏi bằng AI: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            Subjects = await _questionService.GetAllSubjectsAsync();

            var selectedQuestions = GeneratedQuestions.Where(q => q.IsSelected).ToList();
            if (!selectedQuestions.Any())
            {
                StatusMessage = "Error: Bạn chưa chọn câu hỏi nào để lưu.";
                HasGenerated = true; // keep showing the generated questions list
                return Page();
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Challenge();

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null) return Challenge();

            int savedCount = 0;
            foreach (var q in selectedQuestions)
            {
                var createDto = new CreateQuestionDto
                {
                    SubjectId = q.SubjectId,
                    Content = q.Content,
                    QuestionType = q.QuestionType,
                    OptionsJson = q.OptionsJson,
                    CorrectAnswer = q.CorrectAnswer,
                    Difficulty = q.Difficulty,
                    Tags = q.Tags,
                    IsAIGenerated = true // AI generated flag is true
                };

                var success = await _questionService.AddQuestionAsync(createDto, user.Id);
                if (success) savedCount++;
            }

            StatusMessage = $"Thành công: Đã lưu {savedCount}/{selectedQuestions.Count} câu hỏi được chọn vào Ngân hàng câu hỏi.";
            
            // Clear generated questions list on success and redirect back to initial state
            GeneratedQuestions.Clear();
            HasGenerated = false;

            return RedirectToPage();
        }
    }

    public class SelectedQuestionViewModel
    {
        public bool IsSelected { get; set; }
        public int SubjectId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "MultipleChoice";
        public string? OptionsJson { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "Medium";
        public string? Tags { get; set; }
    }
}
