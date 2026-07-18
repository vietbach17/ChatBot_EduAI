using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.Linq;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System;

namespace PresentationLayer.Pages
{
    [Authorize]
    /// <summary>
    /// PageModel trang xem truc tiep tai lieu. Nhung hoac chuyen huong den URL tai lieu, ghi log hoat dong xem tai lieu cua nguoi dung.
    /// </summary>
    public class ViewDocumentModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;

        public ViewDocumentModel(IDocumentService documentService, ISubjectService subjectService)
        {
            _documentService = documentService;
            _subjectService = subjectService;
        }

        public DocumentDto Document { get; set; } = new DocumentDto();
        public string TextContent { get; set; } = string.Empty;
        public List<string> SimulatedChunks { get; set; } = new List<string>();
        public bool CanViewChunks { get; set; } = false;

        // ── Deep-link trích dẫn từ Chat: ?chunk=N → định vị & highlight đoạn đó trong nội dung ──
        public int? HighlightChunkIndex { get; set; }
        public int HighlightStart { get; set; } = -1;
        public int HighlightLength { get; set; }
        public bool HighlightFound => HighlightStart >= 0 && HighlightLength > 0;

        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, int? chunk = null)
        {
            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null)
            {
                return NotFound();
            }

            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            if (doc.IsDeleted)
            {
                if (role == "Admin")
                {
                    // Admin can view deleted
                }
                else if (role == "Lecturer" && doc.SubjectId.HasValue)
                {
                    var uIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                    if (int.TryParse(uIdClaim, out int uid))
                    {
                        var subject = await _subjectService.GetSubjectByIdAsync(doc.SubjectId.Value);
                        if (subject == null || subject.LecturerId != uid)
                        {
                            return Forbid();
                        }
                    }
                    else
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return Forbid();
                }
            }

            Document = doc;
            TextContent = doc.Content ?? await _documentService.GetDocumentTextAsync(id);
            if (string.IsNullOrWhiteSpace(TextContent))
                TextContent = "Không có nội dung dạng text cho file này.";

            role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            
            CanViewChunks = false;
            if (role == "Admin")
            {
                CanViewChunks = true;
            }
            else if (role == "Lecturer" && doc.SubjectId.HasValue)
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    var subject = await _subjectService.GetSubjectByIdAsync(doc.SubjectId.Value);
                    if (subject != null && subject.LecturerId == userId)
                    {
                        CanViewChunks = true;
                    }
                }
            }

            if (CanViewChunks)
            {
                var dbChunks = await _documentService.GetDocumentChunksAsync(id);
                if (dbChunks != null && dbChunks.Any())
                {
                    SimulatedChunks = dbChunks.ToList();
                }
            }

            // Deep-link từ trích dẫn Chat: định vị chunk trong nội dung để highlight
            if (chunk.HasValue && chunk.Value > 0)
            {
                HighlightChunkIndex = chunk.Value;
                var chunkContent = await _documentService.GetChunkByOrderIndexAsync(id, chunk.Value);
                if (!string.IsNullOrWhiteSpace(chunkContent) && !string.IsNullOrWhiteSpace(TextContent))
                    LocateChunkInContent(chunkContent);
            }

            return Page();
        }

        /// <summary>
        /// Tìm vị trí chunk trong nội dung gốc. Chunk được tạo bằng cách nối từ với 1 dấu cách,
        /// còn nội dung gốc giữ nguyên xuống dòng — nên so khớp bằng regex "từ\s+từ\s+..." trên từng từ.
        /// Thử khớp toàn bộ chunk trước, không được thì khớp 40 từ đầu.
        /// </summary>
        private void LocateChunkInContent(string chunkContent)
        {
            var words = chunkContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 3) return;

            foreach (var take in new[] { words.Length, Math.Min(40, words.Length) })
            {
                var pattern = string.Join(@"\s+", words.Take(take).Select(System.Text.RegularExpressions.Regex.Escape));
                try
                {
                    var m = System.Text.RegularExpressions.Regex.Match(TextContent, pattern,
                        System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2));
                    if (m.Success)
                    {
                        HighlightStart = m.Index;
                        HighlightLength = m.Length;
                        return;
                    }
                }
                catch (System.Text.RegularExpressions.RegexMatchTimeoutException) { return; }
            }
        }

        public async Task<IActionResult> OnPostProcessEmbeddingAsync(int id)
        {
            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null) return NotFound();

            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            
            bool canProcess = false;
            if (role == "Admin")
            {
                canProcess = true;
            }
            else if (role == "Lecturer" && doc.SubjectId.HasValue)
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    var subject = await _subjectService.GetSubjectByIdAsync(doc.SubjectId.Value);
                    if (subject != null && subject.LecturerId == userId)
                    {
                        canProcess = true;
                    }
                }
            }

            if (!canProcess)
            {
                return Forbid();
            }

            var result = await _documentService.ProcessDocumentEmbeddingAsync(id);
            if (result)
            {
                SuccessMessage = "Băm và Nhúng Vector thành công!";
            }
            return RedirectToPage(new { id = id });
        }
    }
}
