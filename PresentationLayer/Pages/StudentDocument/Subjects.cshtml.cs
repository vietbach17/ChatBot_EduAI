using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.StudentDocument
{
    [Authorize]
    public class SubjectsModel : PageModel
    {
        private readonly ISubjectService _subjectService;

        public SubjectsModel(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();

        public async Task OnGetAsync()
        {
            Subjects = await _subjectService.GetAllSubjectsAsync();
        }
    }
}
