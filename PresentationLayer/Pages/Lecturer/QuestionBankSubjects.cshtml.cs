using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BussinessLayer.DTOs;
using BussinessLayer.IServices;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    /// <summary>
    /// Trang chon Mon hoc truoc khi vao Ngan hang Cau hoi. Chi hien thi cac mon giang vien dang phu trach.
    /// </summary>
    public class QuestionBankSubjectsModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IUserService _userService;
        private readonly IQuestionBankService _questionService;

        public QuestionBankSubjectsModel(ISubjectService subjectService, IUserService userService, IQuestionBankService questionService)
        {
            _subjectService = subjectService;
            _userService = userService;
            _questionService = questionService;
        }

        public class SubjectBankItem
        {
            public SubjectDto Subject { get; set; } = default!;
            public int QuestionCount { get; set; }
        }

        public List<SubjectBankItem> Subjects { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Challenge();

            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null) return Challenge();

            var subjects = await _subjectService.GetSubjectsByLecturerIdAsync(user.Id);
            foreach (var subject in subjects.Where(s => !s.IsDeleted))
            {
                var paged = await _questionService.GetPagedQuestionsAsync(subject.Id, null, null, null, 1, 1);
                Subjects.Add(new SubjectBankItem { Subject = subject, QuestionCount = paged.TotalCount });
            }

            return Page();
        }
    }
}
