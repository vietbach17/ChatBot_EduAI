using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    public class CreateQuizModel : PageModel
    {
        private readonly IQuizService _quizService;
        private readonly IQuestionBankRepository _questionBankRepo;

        public CreateQuizModel(IQuizService quizService, IQuestionBankRepository questionBankRepo)
        {
            _quizService = quizService;
            _questionBankRepo = questionBankRepo;
        }

        [BindProperty]
        public CreateQuizDto QuizInput { get; set; } = new CreateQuizDto();

        [BindProperty(SupportsGet = true)]
        public int SubjectId { get; set; }

        public string QuestionsJson { get; set; } = "[]";
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (SubjectId <= 0)
            {
                return RedirectToPage("/Lecturer/MySubjects");
            }

            QuizInput.SubjectId = SubjectId;
            QuizInput.NumVariants = 1;

            // Load tất cả câu hỏi của môn học này để JS sử dụng bên cột phải
            var questions = await _questionBankRepo.GetPagedAsync(SubjectId, null, null, null, 1, 1000);
            
            var simpleQuestions = questions.Select(q => new
            {
                id = q.Id,
                questionText = q.Content,
                difficulty = q.Difficulty,
                type = q.QuestionType
            });

            QuestionsJson = JsonSerializer.Serialize(simpleQuestions);

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
                    var variants = JsonSerializer.Deserialize<List<VariantQuestionsDto>>(variantsJson);
                    if (variants != null)
                    {
                        QuizInput.Variants = variants;
                        QuizInput.NumVariants = variants.Count;
                    }
                }
                catch
                {
                    // Ignore JSON errors
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
                await _quizService.CreateQuizAsync(lecturerId, QuizInput);
                return RedirectToPage("/Lecturer/ManageSubject", new { id = SubjectId });
            }
            catch (System.Exception ex)
            {
                ErrorMessage = ex.Message;
                // Reload câu hỏi để khỏi lỗi UI
                var questions = await _questionBankRepo.GetPagedAsync(SubjectId, null, null, null, 1, 1000);
                QuestionsJson = JsonSerializer.Serialize(questions.Select(q => new { id = q.Id, questionText = q.Content, difficulty = q.Difficulty, type = q.QuestionType }));
                return Page();
            }
        }
    }
}
