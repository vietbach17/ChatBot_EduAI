using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Giao diện Repository truy vấn Môn học, Chương, và phân công Giảng viên.
    /// </summary>
    public interface ISubjectRepository
    {
        Task<IEnumerable<Subject>> GetAllSubjectsAsync(bool includeDeleted = false);
        Task<Subject?> GetSubjectByIdAsync(int id);
        Task<IEnumerable<Chapter>> GetChaptersBySubjectIdAsync(int subjectId);
        Task<IEnumerable<Subject>> GetSubjectsByLecturerIdAsync(int lecturerId);
        Task AddSubjectAsync(Subject subject);
        Task UpdateSubjectAsync(Subject subject);
        Task SoftDeleteSubjectAsync(int id);
        Task AddChapterAsync(Chapter chapter);
        Task UpdateChapterAsync(Chapter chapter);
        Task DeleteChapterWithOptionsAsync(int chapterId, bool keepDocuments);
        Task<Chapter?> GetChapterByIdAsync(int id);

        /// <summary>Lấy cấu hình chunk riêng của môn (null nếu môn đang dùng template của Admin).</summary>
        Task<(int? MaxWords, int? OverlapWords)> GetChunkSettingsAsync(int subjectId);

        /// <summary>Đặt cấu hình chunk riêng cho môn. Truyền null cho cả hai để quay về template của Admin.</summary>
        Task<bool> UpdateChunkSettingsAsync(int subjectId, int? maxWords, int? overlapWords);
    }
}
