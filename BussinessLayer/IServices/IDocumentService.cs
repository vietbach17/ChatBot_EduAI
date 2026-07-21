using BussinessLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Quản lý Tài liệu: tải lên, trích xuất, tạo embedding, xóa.
    /// </summary>
    public interface IDocumentService
    {
        Task<IEnumerable<DocumentDto>> GetAllDocumentsAsync();
        Task<IEnumerable<DocumentDto>> GetDocumentsBySubjectAsync(int subjectId);
        Task<IEnumerable<DocumentDto>> GetDocumentsByChapterAsync(int chapterId);
        Task<IEnumerable<DocumentDto>> GetDocumentsByUploaderAsync(int uploaderId);
        Task<DocumentDto?> GetDocumentByIdAsync(int id);
        Task<string> GetDocumentTextAsync(int id);
        Task<int> AddDocumentAsync(string title, string fileType, string fileUrl, int? subjectId, int? chapterId, int? uploaderId);
        Task<int> AddDocumentAsync(string title, string fileType, string fileUrl, int? subjectId, int? chapterId, int? uploaderId, string? extractedContent);
        Task<bool> ProcessDocumentAsync(int documentId, string extractedContent);
        Task<bool> ProcessDocumentEmbeddingAsync(int documentId, System.Func<int, int, Task>? progressCallback = null);
        /// <summary>Xóa chunk cũ và băm/nhúng lại tài liệu theo cấu hình chunk hiện hành của môn.</summary>
        Task<bool> ReprocessDocumentEmbeddingAsync(int documentId, System.Func<int, int, Task>? progressCallback = null);
        Task<bool> UpdateDocumentChapterAsync(int documentId, int? chapterId);
        /// <summary>Cập nhật đường dẫn bản PDF dùng để xem chuẩn (sau khi convert office → PDF).</summary>
        Task<bool> UpdateViewUrlAsync(int documentId, string? viewUrl);
        Task<IEnumerable<string>> GetDocumentChunksAsync(int id);
        /// <summary>Lấy nội dung một chunk theo OrderIndex (dùng cho deep-link trích dẫn từ Chat).</summary>
        Task<string?> GetChunkByOrderIndexAsync(int documentId, int orderIndex);
        Task<bool> DeleteDocumentAsync(int id);
    }
}
