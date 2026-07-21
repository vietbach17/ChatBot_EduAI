using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    /// <summary>
    /// Giao diện dịch vụ Quản lý Môn học và Chương.
    /// </summary>
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync(bool includeDeleted = false);
        Task<SubjectDto?> GetSubjectByIdAsync(int id);
        Task<IEnumerable<SubjectDto>> GetSubjectsByLecturerIdAsync(int lecturerId);
        Task<bool> AddSubjectAsync(string code, string name, int? lecturerId);
        Task<bool> UpdateSubjectAsync(int id, string code, string name, int? lecturerId);
        Task<bool> SoftDeleteSubjectAsync(int id);
        Task<bool> RestoreSubjectAsync(int id);
        Task<bool> AddChapterAsync(int subjectId, string title, int orderIndex);
        Task<bool> UpdateChapterAsync(int chapterId, string title);
        Task<bool> DeleteChapterWithOptionsAsync(int chapterId, bool keepDocuments);

        /// <summary>Lấy cấu hình chunk đang có hiệu lực của một môn kèm chính sách hiện hành của Admin.</summary>
        Task<SubjectChunkSettingsDto> GetChunkSettingsAsync(int subjectId);

        /// <summary>
        /// Cập nhật cấu hình chunk riêng của môn. useCustom = false sẽ xóa cấu hình riêng
        /// và đưa môn về template mặc định của Admin. Trả về (thành công, thông báo lỗi nếu có).
        /// </summary>
        Task<(bool Success, string? Error)> UpdateChunkSettingsAsync(int subjectId, bool useCustom, int maxWords, int overlapWords);
    }
}
