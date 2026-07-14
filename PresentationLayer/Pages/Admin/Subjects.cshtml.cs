using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace PresentationLayer.Pages.Admin
{
    /// <summary>
    /// PageModel trang Quan ly Mon hoc cua Admin. Hien thi danh sach mon hoc, ho tro tao moi, sua, xoa mon hoc va chuong hoc.
    /// </summary>
    public class SubjectsModel : PageModel
    {
        private readonly ISubjectService _subjectService = new MockSubjectService();
        private readonly IUserService _userService;

        public SubjectsModel(IUserService userService)
        {
            _userService = userService;
        }

        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
        public SelectList? Lecturers { get; set; }

        [BindProperty]
        public ViewModels.Admin.SubjectCreateViewModel CreateModel { get; set; } = new ViewModels.Admin.SubjectCreateViewModel();

        [BindProperty]
        public ViewModels.Admin.SubjectUpdateViewModel UpdateModel { get; set; } = new ViewModels.Admin.SubjectUpdateViewModel();

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
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateSubjectAsync()
        {
            if (UpdateModel.Id > 0 && !string.IsNullOrWhiteSpace(UpdateModel.Code) && !string.IsNullOrWhiteSpace(UpdateModel.Name))
            {
                await _subjectService.UpdateSubjectAsync(UpdateModel.Id, UpdateModel.Code, UpdateModel.Name, UpdateModel.LecturerId);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSubjectAsync(int id)
        {
            await _subjectService.SoftDeleteSubjectAsync(id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRestoreSubjectAsync(int id)
        {
            await _subjectService.RestoreSubjectAsync(id);
            return RedirectToPage();
        }
    }

    public interface ISubjectService
    {
        Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync(bool includeDeleted);
        Task AddSubjectAsync(string code, string name, int? lecturerId);
        Task UpdateSubjectAsync(int id, string code, string name, int? lecturerId);
        Task SoftDeleteSubjectAsync(int id);
        Task RestoreSubjectAsync(int id);
    }

    public class MockSubjectService : ISubjectService
    {
        private static readonly List<SubjectDto> _subjects = new()
        {
            new SubjectDto { Id = 1, Code = "PRN222", Name = "C# Nâng cao", LecturerId = 2, LecturerName = "lecturer", IsDeleted = false },
            new SubjectDto { Id = 2, Code = "AI101", Name = "Nhập môn AI", LecturerId = 2, LecturerName = "lecturer", IsDeleted = false }
        };

        public Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync(bool includeDeleted)
        {
            var query = _subjects.AsEnumerable();
            if (!includeDeleted) query = query.Where(s => !s.IsDeleted);
            return Task.FromResult(query);
        }

        public Task AddSubjectAsync(string code, string name, int? lecturerId)
        {
            var id = _subjects.Count > 0 ? _subjects.Max(s => s.Id) + 1 : 1;
            _subjects.Add(new SubjectDto
            {
                Id = id,
                Code = code,
                Name = name,
                LecturerId = lecturerId,
                LecturerName = lecturerId.HasValue ? "lecturer" : null,
                IsDeleted = false
            });
            return Task.CompletedTask;
        }

        public Task UpdateSubjectAsync(int id, string code, string name, int? lecturerId)
        {
            var sub = _subjects.FirstOrDefault(s => s.Id == id);
            if (sub != null)
            {
                sub.Code = code;
                sub.Name = name;
                sub.LecturerId = lecturerId;
                sub.LecturerName = lecturerId.HasValue ? "lecturer" : null;
            }
            return Task.CompletedTask;
        }

        public Task SoftDeleteSubjectAsync(int id)
        {
            var sub = _subjects.FirstOrDefault(s => s.Id == id);
            if (sub != null) sub.IsDeleted = true;
            return Task.CompletedTask;
        }

        public Task RestoreSubjectAsync(int id)
        {
            var sub = _subjects.FirstOrDefault(s => s.Id == id);
            if (sub != null) sub.IsDeleted = false;
            return Task.CompletedTask;
        }
    }
}
