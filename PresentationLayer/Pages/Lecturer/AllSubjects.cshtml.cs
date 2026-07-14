using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.SignalR;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    /// <summary>
    /// PageModel trang Tat ca Mon hoc (danh cho Giang vien). Hien thi toan bo mon hoc trong he thong de giang vien tham khao.
    /// </summary>
    public class AllSubjectsModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IHubContext<SignalRHub> _hubContext;

        public AllSubjectsModel(ISubjectService subjectService, IHubContext<SignalRHub> hubContext)
        {
            _subjectService = subjectService;
            _hubContext = hubContext;
        }

        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();

        public async Task OnGetAsync()
        {
            // GetAllSubjectsAsync(false) để không hiển thị môn đã xóa
            Subjects = await _subjectService.GetAllSubjectsAsync(false);
        }
    }
}
