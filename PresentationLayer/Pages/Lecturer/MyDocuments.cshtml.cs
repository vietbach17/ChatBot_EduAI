using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationLayer.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    /// <summary>
    /// PageModel trang Tai lieu cua toi (danh cho Giang vien). Hien thi danh sach tai lieu giang vien da upload, ho tro xoa va xem trang thai xu ly.
    /// </summary>
    public class MyDocumentsModel : PageModel
    {
        private readonly IDocumentService _documentService;

        public MyDocumentsModel(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();

        public async Task OnGetAsync()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                Documents = await _documentService.GetDocumentsByUploaderAsync(userId);
            }
        }
    }
}
