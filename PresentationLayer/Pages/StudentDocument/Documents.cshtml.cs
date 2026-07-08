using BussinessLayer.DTOs;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

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