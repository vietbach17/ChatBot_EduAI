using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
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
    }
}
