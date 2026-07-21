using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.Linq;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System;
using System.IO;

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
        private readonly IPdfConversionService _pdfConversion;
        private readonly IWebHostEnvironment _env;

        public ViewDocumentModel(IDocumentService documentService, ISubjectService subjectService,
            IPdfConversionService pdfConversion, IWebHostEnvironment env)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _pdfConversion = pdfConversion;
            _env = env;
        }

        public DocumentDto Document { get; set; } = new DocumentDto();
        public string TextContent { get; set; } = string.Empty;
        public List<string> SimulatedChunks { get; set; } = new List<string>();
        public bool CanViewChunks { get; set; } = false;

        // ── Xem "chuẩn như lúc up": URL bản PDF (native hoặc convert từ office) để render bằng PDF.js ──
        public string? PdfUrl { get; set; }
        public bool IsPdfView => !string.IsNullOrEmpty(PdfUrl);

        // ── Deep-link trích dẫn từ Chat: ?chunk=N → định vị & highlight đoạn đó trong nội dung ──
        public int? HighlightChunkIndex { get; set; }
        public string? HighlightChunkText { get; set; } // Nội dung thô của chunk, dùng để highlight trong viewer PDF
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

            // Xác định bản PDF để xem "chuẩn như lúc up"
            PdfUrl = await ResolvePdfUrlAsync(doc);

            // Deep-link từ trích dẫn Chat: highlight đoạn được trích dẫn
            if (chunk.HasValue && chunk.Value > 0)
            {
                HighlightChunkIndex = chunk.Value;
                var chunkContent = await _documentService.GetChunkByOrderIndexAsync(id, chunk.Value);
                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    HighlightChunkText = chunkContent;
                    // Chế độ text: định vị chunk trong nội dung để tô <mark> phía server.
                    // Chế độ PDF: highlight được xử lý phía client bằng HighlightChunkText.
                    if (!IsPdfView && !string.IsNullOrWhiteSpace(TextContent))
                        LocateChunkInContent(chunkContent);
                }
            }

            return Page();
        }

        /// <summary>
        /// Trả về URL bản PDF để render bằng PDF.js:
        /// - File PDF: dùng chính FileUrl.
        /// - File office (DOCX/PPTX/…): dùng ViewUrl đã convert; nếu chưa có thì convert ngay (lazy) và lưu lại.
        /// - Các loại khác (txt/md/csv/log) hoặc không convert được: trả null (rơi về chế độ xem text).
        /// </summary>
        private async Task<string?> ResolvePdfUrlAsync(DocumentDto doc)
        {
            var ft = (doc.FileType ?? string.Empty).ToLowerInvariant();

            if (ft == "pdf")
                return doc.FileUrl;

            if (!_pdfConversion.CanConvert(ft))
                return null;

            // Đã convert trước đó và file còn tồn tại?
            if (!string.IsNullOrEmpty(doc.ViewUrl))
            {
                var existingAbs = MapFilesUrlToAbsolute(doc.ViewUrl);
                if (existingAbs != null && System.IO.File.Exists(existingAbs))
                    return doc.ViewUrl;
            }

            if (!_pdfConversion.IsAvailable)
                return null;

            var sourceAbs = MapFilesUrlToAbsolute(doc.FileUrl);
            if (sourceAbs == null || !System.IO.File.Exists(sourceAbs))
                return null;

            var filesDir = Path.Combine(_env.ContentRootPath, "App_Data", "files");
            var outputPdf = await _pdfConversion.ConvertToPdfAsync(sourceAbs, filesDir);
            if (string.IsNullOrEmpty(outputPdf))
                return null;

            var viewUrl = "/files/" + Path.GetFileName(outputPdf);
            await _documentService.UpdateViewUrlAsync(doc.Id, viewUrl);
            return viewUrl;
        }

        /// <summary>Ánh xạ URL công khai (/files/... hoặc dưới wwwroot) về đường dẫn vật lý tuyệt đối.</summary>
        private string? MapFilesUrlToAbsolute(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            if (url.StartsWith("/files/", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(_env.ContentRootPath, "App_Data", "files", Path.GetFileName(url));

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(webRoot, relative);
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

            // Khớp chính xác thất bại (khác dấu câu, gạch nối, NFC/NFD, khoảng trắng lạ…)
            // → khớp trên bản chuẩn hoá chỉ còn chữ và số, rồi ánh xạ ngược về vị trí gốc.
            LocateChunkNormalized(chunkContent);
        }

        /// <summary>
        /// Chuẩn hoá chuỗi về dạng chỉ chữ+số không dấu, đồng thời trả về map vị trí
        /// ký tự chuẩn hoá thứ i nằm ở đâu trong chuỗi gốc.
        /// </summary>
        private static (string Text, List<int> Map) Normalize(string source)
        {
            var sb = new System.Text.StringBuilder(source.Length);
            var map = new List<int>(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                var c = char.ToLowerInvariant(source[i]);
                if (c == 'đ') c = 'd';
                foreach (var d in c.ToString().Normalize(System.Text.NormalizationForm.FormD))
                {
                    if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(d)
                        == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                    if (!char.IsLetterOrDigit(d)) continue;
                    sb.Append(d);
                    map.Add(i);
                }
            }
            return (sb.ToString(), map);
        }

        /// <summary>
        /// Khớp chunk trong nội dung dựa trên bản chuẩn hoá. Thử nhiều độ dài cửa sổ và
        /// trượt vị trí bắt đầu dọc chunk để bền với sai lệch ở đầu đoạn.
        /// </summary>
        private void LocateChunkNormalized(string chunkContent)
        {
            var (haystack, map) = Normalize(TextContent);
            var (needle, _) = Normalize(chunkContent);
            if (haystack.Length == 0 || needle.Length < 20) return;

            foreach (var candidate in new[] { needle.Length, 300, 200, 120, 80, 50, 30 })
            {
                var len = Math.Min(candidate, needle.Length);
                if (len < 20) continue;
                var step = Math.Max(1, len / 2);
                for (int s = 0; s + len <= needle.Length; s += step)
                {
                    var idx = haystack.IndexOf(needle.Substring(s, len), StringComparison.Ordinal);
                    if (idx < 0) continue;

                    // Nới vùng tô ra cả chunk chứ không chỉ cửa sổ khớp được.
                    var from = Math.Max(0, idx - s);
                    var to = Math.Min(haystack.Length - 1, from + needle.Length - 1);
                    HighlightStart = map[from];
                    HighlightLength = map[to] - map[from] + 1;
                    return;
                }
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
