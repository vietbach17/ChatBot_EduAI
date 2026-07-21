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
    [RequestSizeLimit(1024L * 1024L * 1024L)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024L * 1024L)]
    public class ManageSubjectModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IDocumentService _documentService;
        private readonly IFileTextExtractorService _textExtractor;
        private readonly IHubContext<SignalRHub> _hubContext;
        private readonly IDocumentActivityLogService _activityLogService;
        private readonly IQuizService _quizService;
        private readonly IQuizActivityLogService _quizActivityLogService;

        public ManageSubjectModel(ISubjectService subjectService, IDocumentService documentService, IFileTextExtractorService textExtractor, IHubContext<SignalRHub> hubContext, IDocumentActivityLogService activityLogService, IQuizService quizService, IQuizActivityLogService quizActivityLogService)
        {
            _subjectService = subjectService;
            _documentService = documentService;
            _textExtractor = textExtractor;
            _hubContext = hubContext;
            _activityLogService = activityLogService;
            _quizService = quizService;
            _quizActivityLogService = quizActivityLogService;
        }

        public SubjectDto Subject { get; set; } = default!;
        public List<QuizSummaryDto> Quizzes { get; set; } = new();

        [BindProperty] public ChapterCreateViewModel CreateChapterModel { get; set; } = new ChapterCreateViewModel();
        
        [BindProperty] public ChapterUpdateViewModel UpdateChapterModel { get; set; } = new ChapterUpdateViewModel();
        
        [BindProperty] public DocumentUploadViewModel UploadDocumentModel { get; set; } = new DocumentUploadViewModel();

        public bool IsOwner { get; set; } = false;
        public bool IsAdmin { get; set; } = false;



        public List<ActivityLogItemViewModel> ActivityLogs { get; set; } = new();

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
                var docLogs = await _activityLogService.GetLogsBySubjectIdAsync(id);
                var quizLogs = await _quizActivityLogService.GetLogsBySubjectIdAsync(id);

                ActivityLogs = docLogs.Select(l => new ActivityLogItemViewModel
                {
                    Timestamp = l.Timestamp,
                    UserName = l.UserName,
                    Kind = "Document",
                    Action = l.Action,
                    Title = l.DocumentTitle,
                    LinkId = l.DocumentId
                })
                .Concat(quizLogs.Select(l => new ActivityLogItemViewModel
                {
                    Timestamp = l.Timestamp,
                    UserName = l.UserName,
                    Kind = "Quiz",
                    Action = l.Action,
                    Title = l.QuizTitle,
                    LinkId = l.QuizId
                }))
                .OrderByDescending(l => l.Timestamp)
                .ToList();
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
            var uploadFiles = UploadDocumentModel.Files?
                .Where(file => file != null && file.Length > 0)
                .ToList() ?? new List<IFormFile>();

            if (!uploadFiles.Any() && UploadDocumentModel.File != null && UploadDocumentModel.File.Length > 0)
            {
                uploadFiles.Add(UploadDocumentModel.File);
            }

            if (uploadFiles.Any())
            {
                var filesDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "files");
                Directory.CreateDirectory(filesDir);

                if (UploadDocumentModel.File == null || uploadFiles.Count > 1)
                {
                    int? batchUploaderId = null;
                    var batchUserIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                    if (batchUserIdClaim != null && int.TryParse(batchUserIdClaim, out var batchUId))
                    {
                        batchUploaderId = batchUId;
                    }

                    var connId = Request.Form["UploadDocumentModel.ConnectionId"].FirstOrDefault() ?? UploadDocumentModel.ConnectionId;

                    for (var fileIndex = 0; fileIndex < uploadFiles.Count; fileIndex++)
                    {
                        var uploadFile = uploadFiles[fileIndex];
                        var originalFileName = Path.GetFileName(uploadFile.FileName);
                        var storedFileName = $"{Guid.NewGuid():N}_{originalFileName}";
                        var batchFilePath = Path.Combine(filesDir, storedFileName);

                        using (var stream = new FileStream(batchFilePath, FileMode.Create))
                        {
                            await uploadFile.CopyToAsync(stream);
                        }

                        var batchFileUrl = $"/files/{storedFileName}";
                        var batchFileType = Path.GetExtension(originalFileName).TrimStart('.').ToLower();
                        var batchExtractedContent = _textExtractor.ExtractText(batchFilePath);
                        var documentTitle = Path.GetFileNameWithoutExtension(originalFileName);

                        if (!string.IsNullOrEmpty(connId))
                        {
                            var startPercent = (int)System.Math.Round((double)fileIndex / uploadFiles.Count * 100);
                            await _hubContext.Clients.Client(connId).SendAsync(
                                "UploadProgress",
                                startPercent,
                                $"Đang xử lý file {fileIndex + 1}/{uploadFiles.Count}: {originalFileName}");
                        }

                        var batchDocumentId = await _documentService.AddDocumentAsync(documentTitle, batchFileType, batchFileUrl, id, UploadDocumentModel.ChapterId, batchUploaderId, batchExtractedContent);
                        if (batchDocumentId <= 0)
                        {
                            continue;
                        }

                        try
                        {
                            await _documentService.ProcessDocumentEmbeddingAsync(batchDocumentId, async (current, total) =>
                            {
                                if (!string.IsNullOrEmpty(connId))
                                {
                                    var fileProgress = total > 0 ? (double)current / total : 1;
                                    var overallProgress = (fileIndex + fileProgress) / uploadFiles.Count * 100;
                                    int percent = (int)System.Math.Round(overallProgress);
                                    await _hubContext.Clients.Client(connId).SendAsync(
                                        "UploadProgress",
                                        percent,
                                        $"Đang xử lý file {fileIndex + 1}/{uploadFiles.Count}: {originalFileName}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Upload Warning]: Embedding failed: {ex.Message}");
                        }

                        if (batchUploaderId.HasValue)
                        {
                            await _activityLogService.LogActivityAsync(id, batchDocumentId, documentTitle, batchUploaderId.Value, "Uploaded");
                        }
                    }

                    if (!string.IsNullOrEmpty(connId))
                    {
                        await _hubContext.Clients.Client(connId).SendAsync("UploadProgress", 100, "Hoan tat xu ly tat ca file.");
                    }

                    await _hubContext.Clients.All.SendAsync("CourseChanged");
                    return RedirectToPage(new { id = id });
                }

                var fileName = Path.GetFileName(UploadDocumentModel.File.FileName);
                var filePath = Path.Combine(filesDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadDocumentModel.File.CopyToAsync(stream);
                }

                var fileUrl = $"/files/{fileName}";
                var fileType = Path.GetExtension(fileName).TrimStart('.').ToLower();
                var fallbackDocumentTitle = Path.GetFileNameWithoutExtension(fileName);

                // Use FileTextExtractorService (supports txt, md, csv, pdf via PdfPig)
                var extractedContent = _textExtractor.ExtractText(filePath);

                int? uploaderId = null;
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && int.TryParse(userIdClaim, out var uId))
                {
                    uploaderId = uId;
                }

                var documentId = await _documentService.AddDocumentAsync(fallbackDocumentTitle, fileType, fileUrl, id, UploadDocumentModel.ChapterId, uploaderId, extractedContent);
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
                                await _hubContext.Clients.Client(connId).SendAsync("UploadProgress", percent, $"Đang xử lý file 1/1: {fileName}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Upload Warning]: Embedding failed: {ex.Message}");
                    }
                    
                    if (uploaderId.HasValue)
                    {
                        await _activityLogService.LogActivityAsync(id, documentId, fallbackDocumentTitle, uploaderId.Value, "Uploaded");
                    }

                    if (!string.IsNullOrEmpty(connId))
                    {
                        await _hubContext.Clients.Client(connId).SendAsync("UploadProgress", 100, "Hoan tat xu ly file.");
                    }
                }
                await _hubContext.Clients.All.SendAsync("CourseChanged");
            }

            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnGetDownloadAllAsync(int id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null) return NotFound();

            var zipStream = Export.SubjectZipBuilder.Build(subject, Directory.GetCurrentDirectory());
            if (zipStream == null)
            {
                return RedirectToPage(new { id });
            }

            return File(zipStream, "application/zip", $"{subject.Code}_TaiLieu.zip");
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

        public async Task<IActionResult> OnPostDeleteQuizAsync(int id, int quizId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out var uId))
            {
                try
                {
                    var quizInfo = await _quizService.GetQuizForUpdateAsync(uId, quizId);
                    await _quizService.DeleteQuizAsync(uId, quizId);
                    await _quizActivityLogService.LogActivityAsync(id, quizId, quizInfo.Title, uId, "Deleted");
                    await _hubContext.Clients.All.SendAsync("CourseChanged");
                }
                catch (Exception ex)
                {
                    // Có thể thêm TempData["ErrorMessage"] = ex.Message;
                }
            }
            return RedirectToPage(new { id = id });
        }



        public async Task<IActionResult> OnPostReprocessDocumentAsync(int id, int docId)
        {
            if (!await CanConfigureAsync(id)) return Forbid();

            try
            {
                var doc = await _documentService.GetDocumentByIdAsync(docId);
                await _documentService.ReprocessDocumentEmbeddingAsync(docId);
                TempData["SuccessMessage"] = $"Đã xử lý lại tài liệu '{doc?.Title}' theo cấu hình chunk hiện tại.";

                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (doc != null && userIdClaim != null && int.TryParse(userIdClaim, out var uId))
                {
                    await _activityLogService.LogActivityAsync(id, docId, doc.Title, uId, "Reindexed");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Xử lý lại tài liệu thất bại: {ex.Message}";
            }

            return RedirectToPage(new { id = id });
        }



        /// <summary>Chỉ Giảng viên phụ trách môn hoặc Admin được đổi cấu hình chunk / xử lý lại tài liệu.</summary>
        private async Task<bool> CanConfigureAsync(int subjectId)
        {
            if (User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value == "Admin") return true;

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return false;

            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out int userId) && subject.LecturerId == userId;
        }
    }
}
