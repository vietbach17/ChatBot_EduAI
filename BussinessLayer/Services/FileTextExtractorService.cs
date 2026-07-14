using System;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

using BussinessLayer.IServices;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dich vu trich xuat noi dung van ban tu cac loai file. Ho tro PDF (PdfPig), Word (DocX), TXT va cac dinh dang pho bien khac.
    /// </summary>
    public class FileTextExtractorService : IFileTextExtractorService
    {
        /// <summary>
        /// Extracts plain text from the file at the given path.
        /// Supports: .txt, .md, .csv, .pdf, .docx, .doc, .pptx
        /// </summary>
        public string ExtractText(string filePath)
        {
            var ext = Path.GetExtension(filePath).TrimStart('.').ToLower();

            return ext switch
            {
                "txt" or "md" or "csv" or "log" => File.ReadAllText(filePath),
                "pdf"                            => ExtractPdfText(filePath),
                "docx" or "doc"                  => ExtractDocxText(filePath),
                "pptx"                           => ExtractPptxText(filePath),
                _ => $"[File: {Path.GetFileName(filePath)}]\nLoại file '{ext.ToUpper()}' chưa hỗ trợ trích xuất text tự động."
            };
        }

        // ── PDF ──────────────────────────────────────────────────────────────
        private static string ExtractPdfText(string filePath)
        {
            var sb = new StringBuilder();
            try
            {
                using var document = PdfDocument.Open(filePath);
                int pageNum = 1;
                foreach (Page page in document.GetPages())
                {
                    sb.AppendLine($"--- Trang {pageNum} ---");
                    var words = page.GetWords();
                    var lines = words
                        .GroupBy(w => (int)Math.Round(w.BoundingBox.Bottom, 0))
                        .OrderByDescending(g => g.Key)
                        .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            sb.AppendLine(line);
                    }
                    pageNum++;
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[Lỗi khi đọc PDF: {ex.Message}]");
            }
            return sb.ToString();
        }

        // ── DOCX ─────────────────────────────────────────────────────────────
        private static string ExtractDocxText(string filePath)
        {
            var sb = new StringBuilder();
            try
            {
                using var doc = WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return "[Tài liệu Word trống hoặc không hợp lệ]";

                foreach (var para in body.Elements<Paragraph>())
                {
                    var text = para.InnerText;
                    sb.AppendLine(text);
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[Lỗi khi đọc DOCX: {ex.Message}]");
            }
            return sb.ToString();
        }

        // ── PPTX ─────────────────────────────────────────────────────────────
        private static string ExtractPptxText(string filePath)
        {
            var sb = new StringBuilder();
            try
            {
                using var prs = PresentationDocument.Open(filePath, false);
                var presentationPart = prs.PresentationPart;
                if (presentationPart == null) return "[Bài trình chiếu trống hoặc không hợp lệ]";

                var slideIds = presentationPart.Presentation?.SlideIdList?.Elements<SlideId>().ToList();
                if (slideIds == null) return "[Không có slide nào]";

                int slideNum = 1;
                foreach (var slideId in slideIds)
                {
                    sb.AppendLine($"--- Slide {slideNum} ---");
                    var rId = slideId.RelationshipId?.Value;
                    if (rId == null) { slideNum++; continue; }

                    var slidePart = (SlidePart)presentationPart.GetPartById(rId);
                    if (slidePart?.Slide != null)
                    {
                        // Extract all text shapes from the slide
                        foreach (var shape in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                        {
                            var text = shape.InnerText;
                            if (!string.IsNullOrWhiteSpace(text))
                                sb.AppendLine(text);
                        }
                    }
                    slideNum++;
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[Lỗi khi đọc PPTX: {ex.Message}]");
            }
            return sb.ToString();
        }
    }
}

