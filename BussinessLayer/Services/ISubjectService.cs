using System.Collections.Generic;
using System.Threading.Tasks;
using BussinessLayer.DTOs;

namespace BussinessLayer.Services
{
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
    }
}
