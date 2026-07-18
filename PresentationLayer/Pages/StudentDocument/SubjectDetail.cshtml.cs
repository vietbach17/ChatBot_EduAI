using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.StudentDocument
{
    [Authorize]
    /// <summary>
    /// PageModel trang Chi tiet Mon hoc (Student). Hien thi chuong hoc va tai lieu thuoc mon hoc nguoi dung chon.
    /// </summary>
    public class SubjectDetailModel : PageModel
    {
        private readonly ISubjectService _subjectService;

        public SubjectDetailModel(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        public SubjectDto Subject { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Subject = await _subjectService.GetSubjectByIdAsync(id);
            if (Subject == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnGetDownloadAllAsync(int id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null) return NotFound();

            var zipStream = Export.SubjectZipBuilder.Build(subject, System.IO.Directory.GetCurrentDirectory());
            if (zipStream == null)
            {
                return RedirectToPage(new { id });
            }

            return File(zipStream, "application/zip", $"{subject.Code}_TaiLieu.zip");
        }
    }
}
