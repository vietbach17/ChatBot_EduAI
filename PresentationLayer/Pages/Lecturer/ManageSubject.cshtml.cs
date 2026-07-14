using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.SignalR;
using PresentationLayer.ViewModels.Lecturer;

namespace PresentationLayer.Pages.Lecturer
{
    /// <summary>
    /// PageModel trang Quan ly Chi tiet Mon hoc (danh cho Giang vien). Cho phep giang vien them/sua/xoa chuong hoc va upload tai lieu.
    /// </summary>
    public class ManageSubjectModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IDocumentService _documentService;
        private readonly IFileTextExtractorService _textExtractor;
        private readonly IHubContext<SignalRHub> _hubContext;
        private readonly IDocumentActivityLogService _activityLogService;
        private readonly IQuizService _quizService;

        public ManageSubjectModel(ISubjectService subjectService, IDocumentService documentService, IFileTextExtractorService textExtractor, IHubContext<SignalRHub> hubContext, IDocumentActivityLogService activityLogService, IQuizService quizService)
        {
            _subjectService = subjectService;
            _documentService = documentService;
            _textExtractor = textExtractor;
            _hubContext = hubContext;
            _activityLogService = activityLogService;
            _quizService = quizService;
        }

        public SubjectDto Subject { get; set; } = default!;
        public List<QuizSummaryDto> Quizzes { get; set; } = new();

        [BindProperty] public ChapterCreateViewModel CreateChapterModel { get; set; } = new ChapterCreateViewModel();
        
        [BindProperty] public ChapterUpdateViewModel UpdateChapterModel { get; set; } = new ChapterUpdateViewModel();
        
        [BindProperty] public DocumentUploadViewModel UploadDocumentModel { get; set; } = new DocumentUploadViewModel();

        public bool IsOwner { get; set; } = false;
        public bool IsAdmin { get; set; } = false;
        
        public List<DocumentActivityLogDto> ActivityLogs { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Subject = await _subjectService.GetSubjectByIdAsync(id);
            if (Subject == null) return NotFound();

            // Lấy LecturerId từ claims
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                if (Subject.LecturerId == userId)
                {
                    IsOwner = true;
                }
            }
            
            if (User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value == "Admin")
            {
                IsAdmin = true;
            }

            Quizzes = await _quizService.GetQuizzesBySubjectAsync(id);

            if (IsOwner || IsAdmin)
            {
                ActivityLogs = (await _activityLogService.GetLogsBySubjectIdAsync(id)).ToList();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddChapterAsync(int id)
        {
            if (!string.IsNullOrWhiteSpace(CreateChapterModel.Title))
            {
                var subject = await _subjectService.GetSubjectByIdAsync(id);
                int order = (subject?.Chapters?.Count ?? 0) + 1;
                await _subjectService.AddChapterAsync(id, CreateChapterModel.Title, order);
                await _hubContext.Clients.All.SendAsync("CourseChanged");
            }
            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostUpdateChapterAsync(int id)
        {
            if (UpdateChapterModel.Id > 0 && !string.IsNullOrWhiteSpace(UpdateChapterModel.Title))
            {
                await _subjectService.UpdateChapterAsync(UpdateChapterModel.Id, UpdateChapterModel.Title);
                await _hubContext.Clients.All.SendAsync("CourseChanged");
            }
            return RedirectToPage(new { id = id });
        }

        [BindProperty] public ChapterDeleteViewModel DeleteChapterModel { get; set; } = new ChapterDeleteViewModel();

        public async Task<IActionResult> OnPostDeleteChapterAsync(int id, int chapterId)
        {
            bool keepDocuments = DeleteChapterModel.Option == "keep";
            await _subjectService.DeleteChapterWithOptionsAsync(chapterId, keepDocuments);
            await _hubContext.Clients.All.SendAsync("CourseChanged");
            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostUploadFileAsync(int id)
        {
            if (UploadDocumentModel.File != null && UploadDocumentModel.File.Length > 0 && !string.IsNullOrWhiteSpace(UploadDocumentModel.Title))
            {
                var filesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files");
                Directory.CreateDirectory(filesDir);
                var fileName = Path.GetFileName(UploadDocumentModel.File.FileName);
                var filePath = Path.Combine(filesDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadDocumentModel.File.CopyToAsync(stream);
                }

                var fileUrl = $"/files/{fileName}";
                var fileType = Path.GetExtension(fileName).TrimStart('.').ToLower();

                // Use FileTextExtractorService (supports txt, md, csv, pdf via PdfPig)
                var extractedContent = _textExtractor.ExtractText(filePath);

                int? uploaderId = null;
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && int.TryParse(userIdClaim, out var uId))
                {
                    uploaderId = uId;
                }

                var documentId = await _documentService.AddDocumentAsync(UploadDocumentModel.Title, fileType, fileUrl, id, UploadDocumentModel.ChapterId, uploaderId, extractedContent);
                if (documentId > 0)
                {
                    // Đọc ConnectionId từ Form (đề phòng BindProperty không bắt được do multipart)
                    var connId = Request.Form["UploadDocumentModel.ConnectionId"].FirstOrDefault() ?? UploadDocumentModel.ConnectionId;

                    try 
                    {
                        // Tự động băm và nhúng ngay lập tức với progress callback
                        await _documentService.ProcessDocumentEmbeddingAsync(documentId, async (current, total) => 
                        {
                            if (!string.IsNullOrEmpty(connId))
                            {
                                int percent = (int)System.Math.Round((double)current / total * 100);
                                await _hubContext.Clients.Client(connId).SendAsync("UploadProgress", percent);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Upload Warning]: Embedding failed: {ex.Message}");
                    }
                    
                    if (uploaderId.HasValue)
                    {
                        await _activityLogService.LogActivityAsync(id, documentId, UploadDocumentModel.Title, uploaderId.Value, "Uploaded");
                    }
                }
                await _hubContext.Clients.All.SendAsync("CourseChanged");
            }

            return RedirectToPage(new { id = id });
        }

        [BindProperty] public DocumentMoveViewModel MoveDocumentModel { get; set; } = new DocumentMoveViewModel();

        public async Task<IActionResult> OnPostMoveDocumentAsync(int id)
        {
            if (MoveDocumentModel.DocumentId > 0)
            {
                var doc = await _documentService.GetDocumentByIdAsync(MoveDocumentModel.DocumentId);
                await _documentService.UpdateDocumentChapterAsync(MoveDocumentModel.DocumentId, MoveDocumentModel.ToChapterId);
                
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (doc != null && userIdClaim != null && int.TryParse(userIdClaim, out var uId))
                {
                    await _activityLogService.LogActivityAsync(id, MoveDocumentModel.DocumentId, doc.Title, uId, "Moved");
                }
                
                await _hubContext.Clients.All.SendAsync("CourseChanged");
            }
            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostDeleteDocumentAsync(int id, int docId)
        {
            var doc = await _documentService.GetDocumentByIdAsync(docId);
            await _documentService.DeleteDocumentAsync(docId);
            
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (doc != null && userIdClaim != null && int.TryParse(userIdClaim, out var uId))
            {
                await _activityLogService.LogActivityAsync(id, docId, doc.Title, uId, "Deleted");
            }
            
            await _hubContext.Clients.All.SendAsync("CourseChanged");
            return RedirectToPage(new { id = id });
        }
    }
}
