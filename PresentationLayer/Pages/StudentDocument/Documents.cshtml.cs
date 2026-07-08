using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PresentationLayer.Pages.StudentDocument
{
    [Authorize]
    public class DocumentsModel : PageModel
    {
        private readonly IDocumentService _documentService;

        public DocumentsModel(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();

        public async Task OnGetAsync()
        {
            Documents = (await _documentService.GetAllDocumentsAsync()).ToList();
        }
    }
}