using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.SignalR;
using PresentationLayer.ViewModels.Admin;

namespace PresentationLayer.Pages.Admin
{
    /// <summary>
    /// PageModel trang Quan ly Mon hoc cua Admin. Hien thi danh sach mon hoc, ho tro tao moi, sua, xoa mon hoc va chuong hoc.
    /// </summary>
    public class SubjectsModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IUserService _userService;
        private readonly IHubContext<SignalRHub> _hubContext;

        public SubjectsModel(ISubjectService subjectService, IUserService userService, IHubContext<SignalRHub> hubContext)
        {
            _subjectService = subjectService;
            _userService = userService;
            _hubContext = hubContext;
        }

        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
        public SelectList? Lecturers { get; set; }

        [BindProperty]
        public SubjectCreateViewModel CreateModel { get; set; } = new SubjectCreateViewModel();

        [BindProperty]
        public SubjectUpdateViewModel UpdateModel { get; set; } = new SubjectUpdateViewModel();

        public async Task OnGetAsync()
        {
            Subjects = await _subjectService.GetAllSubjectsAsync(true); // Include deleted
            var lecturers = await _userService.GetLecturersAsync();
            Lecturers = new SelectList(lecturers, "Id", "Username");
        }

        public async Task<IActionResult> OnPostAddSubjectAsync()
        {
            if (!string.IsNullOrWhiteSpace(CreateModel.Code) && !string.IsNullOrWhiteSpace(CreateModel.Name))
            {
                await _subjectService.AddSubjectAsync(CreateModel.Code, CreateModel.Name, CreateModel.LecturerId);
                await _hubContext.Clients.All.SendAsync("CourseChanged");
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateSubjectAsync()
        {
            if (UpdateModel.Id > 0 && !string.IsNullOrWhiteSpace(UpdateModel.Code) && !string.IsNullOrWhiteSpace(UpdateModel.Name))
            {
                await _subjectService.UpdateSubjectAsync(UpdateModel.Id, UpdateModel.Code, UpdateModel.Name, UpdateModel.LecturerId);
                await _hubContext.Clients.All.SendAsync("CourseChanged");
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSubjectAsync(int id)
        {
            await _subjectService.SoftDeleteSubjectAsync(id);
            await _hubContext.Clients.All.SendAsync("CourseChanged");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRestoreSubjectAsync(int id)
        {
            await _subjectService.RestoreSubjectAsync(id);
            await _hubContext.Clients.All.SendAsync("CourseChanged");
            return RedirectToPage();
        }
    }
}
