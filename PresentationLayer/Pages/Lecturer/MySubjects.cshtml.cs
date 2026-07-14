using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.SignalR;
using PresentationLayer.ViewModels.Lecturer;

namespace PresentationLayer.Pages.Lecturer
{
    /// <summary>
    /// PageModel trang Mon hoc cua toi (danh cho Giang vien). Hien thi cac mon hoc duoc phan cong cho giang vien dang dang nhap.
    /// </summary>
    public class MySubjectsModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IHubContext<SignalRHub> _hubContext;

        public MySubjectsModel(ISubjectService subjectService, IHubContext<SignalRHub> hubContext)
        {
            _subjectService = subjectService;
            _hubContext = hubContext;
        }

        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();

        [BindProperty] public SubjectUpdateViewModel UpdateModel { get; set; } = new SubjectUpdateViewModel();

        public async Task OnGetAsync()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                Subjects = await _subjectService.GetSubjectsByLecturerIdAsync(userId);
            }
        }

        public async Task<IActionResult> OnPostUpdateSubjectAsync()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (int.TryParse(userIdStr, out int userId) && UpdateModel.Id > 0 && !string.IsNullOrWhiteSpace(UpdateModel.Code) && !string.IsNullOrWhiteSpace(UpdateModel.Name))
            {
                // Verify lecturer owns the subject
                var subject = await _subjectService.GetSubjectByIdAsync(UpdateModel.Id);
                if (subject != null && subject.LecturerId == userId)
                {
                    await _subjectService.UpdateSubjectAsync(UpdateModel.Id, UpdateModel.Code, UpdateModel.Name, userId);
                    await _hubContext.Clients.All.SendAsync("CourseChanged");
                }
            }
            return RedirectToPage();
        }
    }
}
