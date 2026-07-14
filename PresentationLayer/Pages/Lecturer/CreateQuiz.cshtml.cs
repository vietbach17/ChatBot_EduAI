using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    [Authorize(Roles = "Lecturer")]
    /// <summary>PageModel trang Tạo bài thi (Giảng viên). Cấu hình bài thi, chia mã đề từ ngân hàng câu hỏi và lưu.</summary>
    public class CreateQuizModel : PageModel
    {
        private readonly IQuizService _quizService;
        private readonly IQuestionBankService _questionBankService;
        private readonly ISubjectService _subjectService;
        private readonly IQuizActivityLogService _activityLogService;
        private readonly IHubContext<SignalRHub> _hubContext;

        public CreateQuizModel(IQuizService quizService, IQuestionBankService questionBankService, ISubjectService subjectService, IQuizActivityLogService activityLogService, IHubContext<SignalRHub> hubContext)
        {
            _quizService = quizService;
            _questionBankService = questionBankService;
            _subjectService = subjectService;
            _activityLogService = activityLogService;
            _hubContext = hubContext;
        }

        private async Task<string> LoadQuestionsJsonAsync()
        {
            var (items, _) = await _questionBankService.GetPagedQuestionsAsync(SubjectId, null, null, null, 1, 1000);
            return JsonSerializer.Serialize(items.Select(q => new
            {
                id = q.Id,
                questionText = q.Content,
                difficulty = q.Difficulty,
                type = q.QuestionType
            }));
        }

        [BindProperty]
        public CreateQuizDto QuizInput { get; set; } = new CreateQuizDto();

        [BindProperty(SupportsGet = true)]
        public int SubjectId { get; set; }

        public string SubjectCode { get; set; } = string.Empty;
        public string QuestionsJson { get; set; } = "[]";
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (SubjectId <= 0)
            {
                return RedirectToPage("/Lecturer/MySubjects");
            }

            var subject = await _subjectService.GetSubjectByIdAsync(SubjectId);
            if (subject != null)
                SubjectCode = subject.Code;

            QuizInput.SubjectId = SubjectId;
            QuizInput.NumVariants = 1;

            // Load tất cả câu hỏi của môn học này để JS sử dụng bên cột phải
            QuestionsJson = await LoadQuestionsJsonAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Trong thực tế sẽ deserialize Variants từ JS gửi lên thông qua 1 hidden input
            var variantsJson = Request.Form["VariantsJson"].ToString();
            if (!string.IsNullOrEmpty(variantsJson))
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var variants = JsonSerializer.Deserialize<List<VariantQuestionsDto>>(variantsJson, options);
                    if (variants != null)
                    {
                        QuizInput.Variants = variants;
                        QuizInput.NumVariants = variants.Count;
                    }
                }
                catch (System.Exception ex)
                {
                    ErrorMessage = "Lỗi JSON: " + ex.Message + " | JSON: " + variantsJson;
                    QuestionsJson = await LoadQuestionsJsonAsync();
                    return Page();
                }
            }

            // Lấy LecturerId từ Claims (Giả định là 1 nếu test)
            int lecturerId = 1; 
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            {
                lecturerId = uid;
            }

            try
            {
                QuizInput.SubjectId = SubjectId;
                var quizId = await _quizService.CreateQuizAsync(lecturerId, QuizInput);
                await _activityLogService.LogActivityAsync(SubjectId, quizId, QuizInput.Title, lecturerId, "Created");
                await _hubContext.Clients.All.SendAsync("CourseChanged");
                TempData["SuccessMessage"] = "Bạn đã tạo đề thi thành công!";
                return RedirectToPage("/Lecturer/CreateQuiz", new { SubjectId = SubjectId });
            }
            catch (System.Exception ex)
            {
                ErrorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                // Reload câu hỏi để khỏi lỗi UI
                QuestionsJson = await LoadQuestionsJsonAsync();
                return Page();
            }
        }
    }
}
