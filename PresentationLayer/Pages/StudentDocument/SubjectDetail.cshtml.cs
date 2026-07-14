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
    }
}
