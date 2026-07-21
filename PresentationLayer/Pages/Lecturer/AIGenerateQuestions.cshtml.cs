using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using PresentationLayer.ViewModels.Lecturer;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    /// <summary>
    /// PageModel trang Sinh cau hoi bang AI (danh cho Giang vien). Cho phep chon mon hoc, nhap yeu cau va goi AI de sinh cau hoi trac nghiem tu dong.
    /// </summary>
    public class AIGenerateQuestionsModel : PageModel
    {
        private readonly IAIQuizGeneratorService _aiGeneratorService;
        private readonly IQuestionBankService _questionService;
        private readonly IUserService _userService;
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;

        public AIGenerateQuestionsModel(
            IAIQuizGeneratorService aiGeneratorService,
            IQuestionBankService questionService,
            IUserService userService,
            IDocumentService documentService,
            ISubjectService subjectService)
        {
            _aiGeneratorService = aiGeneratorService;
            _questionService = questionService;
            _userService = userService;
            _documentService = documentService;
            _subjectService = subjectService;
        }

        [BindProperty]
        public AIGenerateRequestDto GenerateRequest { get; set; } = new AIGenerateRequestDto();

        [BindProperty]
        public List<SelectedQuestionViewModel> GeneratedQuestions { get; set; } = new List<SelectedQuestionViewModel>();

        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
        public IEnumerable<AIGenerationLogDto> GenerationLogs { get; set; } = new List<AIGenerationLogDto>();

        [TempData]
        public string? StatusMessage { get; set; }

        public bool HasGenerated { get; set; } = false;

        private async Task<UserDto?> GetLecturerAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return null;
            return await _userService.GetUserByUsernameAsync(username);
        }

        private async Task LoadDataAsync(int lecturerId)
        {
            var lecturerSubjects = await _subjectService.GetSubjectsByLecturerIdAsync(lecturerId);
            Subjects = lecturerSubjects.Where(s => !s.IsDeleted).ToList();
            GenerationLogs = await _aiGeneratorService.GetGenerationLogsAsync(lecturerId);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await GetLecturerAsync();
            if (user == null) return Challenge();

            await LoadDataAsync(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            var user = await GetLecturerAsync();
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                await LoadDataAsync(user.Id);
                StatusMessage = "Error: Dữ liệu cấu hình yêu cầu không hợp lệ.";
                return Page();
            }

            var ownedSubjects = await _subjectService.GetSubjectsByLecturerIdAsync(user.Id);
            if (!ownedSubjects.Any(s => s.Id == GenerateRequest.SubjectId && !s.IsDeleted))
            {
                await LoadDataAsync(user.Id);
                StatusMessage = "Error: Bạn không được phép tạo câu hỏi cho môn học này vì bạn không phụ trách môn học này.";
                return Page();
            }

            try
            {
                var aiResults = await _aiGeneratorService.GenerateQuestionsAsync(GenerateRequest, user.Id);
                
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

            await LoadDataAsync(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var user = await GetLecturerAsync();
            if (user == null) return Challenge();

            var selectedQuestions = GeneratedQuestions.Where(q => q.IsSelected).ToList();
            if (!selectedQuestions.Any())
            {
                await LoadDataAsync(user.Id);
                StatusMessage = "Error: Bạn chưa chọn câu hỏi nào để lưu.";
                HasGenerated = true; // keep showing the generated questions list
                return Page();
            }

            var ownedSubjects = await _subjectService.GetSubjectsByLecturerIdAsync(user.Id);
            var subjectId = selectedQuestions.First().SubjectId;
            if (!ownedSubjects.Any(s => s.Id == subjectId && !s.IsDeleted))
            {
                await LoadDataAsync(user.Id);
                StatusMessage = "Error: Bạn không được phép lưu câu hỏi cho môn học này vì bạn không phụ trách môn học này.";
                HasGenerated = true;
                return Page();
            }

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

        public async Task<IActionResult> OnGetLogDetailsAsync(int logId)
        {
            var user = await GetLecturerAsync();
            if (user == null) return new JsonResult(new { success = false, message = "Unauthorized" });

            var logs = await _aiGeneratorService.GetGenerationLogsAsync(user.Id);
            var target = logs.FirstOrDefault(l => l.Id == logId);
            if (target == null) return new JsonResult(new { success = false, message = "Không tìm thấy gói câu hỏi." });

            return new JsonResult(new {
                success = true,
                topic = target.Topic,
                subjectName = target.SubjectName,
                difficulty = target.Difficulty,
                questionType = target.QuestionType,
                createdAt = target.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                questions = target.GeneratedQuestionsJson
            });
        }

        public async Task<IActionResult> OnGetSubjectChaptersAsync(int subjectId)
        {
            var user = await GetLecturerAsync();
            if (user == null) return new JsonResult(new { success = false, message = "Unauthorized" });

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return new JsonResult(new { success = false, chapters = new List<object>() });

            var chapters = subject.Chapters
                .Select(c => new { id = c.Id, title = c.Title })
                .ToList();

            return new JsonResult(new { success = true, chapters = chapters });
        }

        public async Task<IActionResult> OnGetSubjectDocumentsAsync(int subjectId, int? chapterId)
        {
            var user = await GetLecturerAsync();
            if (user == null) return new JsonResult(new { success = false, message = "Unauthorized" });

            IEnumerable<DocumentDto> docs;
            if (chapterId.HasValue && chapterId.Value > 0)
            {
                docs = await _documentService.GetDocumentsByChapterAsync(chapterId.Value);
            }
            else
            {
                docs = await _documentService.GetDocumentsBySubjectAsync(subjectId);
            }

            var result = docs
                .Where(d => d.Status.ToString() == "Indexed")
                .Select(d => new { id = d.Id, title = d.Title })
                .ToList();

            return new JsonResult(new { success = true, documents = result });
        }
    }
}
