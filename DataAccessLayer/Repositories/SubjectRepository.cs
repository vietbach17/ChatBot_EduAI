using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    /// <summary>
    /// Repository truy vấn Môn học, Chương, quản lý phân công Giảng viên.
    /// </summary>
    public class SubjectRepository : ISubjectRepository
    {
        private readonly ApplicationDbContext _context;

        public SubjectRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync(bool includeDeleted = false)
        {
            var query = _context.Subjects
                .Include(s => s.Chapters)
                    .ThenInclude(c => c.Documents)
                        .ThenInclude(d => d.Uploader)
                .Include(s => s.Documents)
                    .ThenInclude(d => d.Uploader)
                .Include(s => s.Lecturer)
                .AsQueryable();
                
            if (!includeDeleted)
            {
                query = query.Where(s => !s.IsDeleted);
            }
            
            return await query.AsSplitQuery().ToListAsync();
        }

        public async Task<Subject?> GetSubjectByIdAsync(int id)
        {
            return await _context.Subjects
                .Include(s => s.Chapters)
                    .ThenInclude(c => c.Documents)
                        .ThenInclude(d => d.Uploader)
                .Include(s => s.Documents)
                    .ThenInclude(d => d.Uploader)
                .Include(s => s.Lecturer)
                .AsSplitQuery()
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Chapter>> GetChaptersBySubjectIdAsync(int subjectId)
        {
            return await _context.Chapters
                .Include(c => c.Documents)
                .Where(c => c.SubjectId == subjectId)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subject>> GetSubjectsByLecturerIdAsync(int lecturerId)
        {
            return await _context.Subjects
                .Include(s => s.Chapters)
                    .ThenInclude(c => c.Documents)
                        .ThenInclude(d => d.Uploader)
                .Include(s => s.Documents)
                    .ThenInclude(d => d.Uploader)
                .Where(s => s.LecturerId == lecturerId && !s.IsDeleted)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task AddSubjectAsync(Subject subject)
        {
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSubjectAsync(Subject subject)
        {
            _context.Subjects.Update(subject);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteSubjectAsync(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Chapters)
                    .ThenInclude(c => c.Documents)
                .Include(s => s.Documents)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject != null)
            {
                subject.IsDeleted = true;

                // Soft delete chapters
                if (subject.Chapters != null)
                {
                    foreach (var chapter in subject.Chapters)
                    {
                        chapter.IsDeleted = true;
                        if (chapter.Documents != null)
                        {
                            var docIds = chapter.Documents.Select(d => d.Id).ToList();
                            var chunks = await _context.DocumentChunks.Where(c => docIds.Contains(c.DocumentId)).ToListAsync();
                            if (chunks.Any())
                            {
                                _context.DocumentChunks.RemoveRange(chunks);
                            }

                            foreach (var doc in chapter.Documents)
                            {
                                doc.IsDeleted = true;
                            }
                            _context.Documents.UpdateRange(chapter.Documents);
                        }
                    }
                    _context.Chapters.UpdateRange(subject.Chapters);
                }

                // Soft delete general documents (not in any chapter)
                if (subject.Documents != null)
                {
                    var generalDocs = subject.Documents.Where(d => d.ChapterId == null).ToList();
                    if (generalDocs.Any())
                    {
                        var docIds = generalDocs.Select(d => d.Id).ToList();
                        var chunks = await _context.DocumentChunks.Where(c => docIds.Contains(c.DocumentId)).ToListAsync();
                        if (chunks.Any())
                        {
                            _context.DocumentChunks.RemoveRange(chunks);
                        }

                        foreach (var doc in generalDocs)
                        {
                            doc.IsDeleted = true;
                        }
                        _context.Documents.UpdateRange(generalDocs);
                    }
                }

                _context.Subjects.Update(subject);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddChapterAsync(Chapter chapter)
        {
            _context.Chapters.Add(chapter);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateChapterAsync(Chapter chapter)
        {
            _context.Chapters.Update(chapter);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteChapterWithOptionsAsync(int chapterId, bool keepDocuments)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == chapterId);

            if (chapter != null)
            {
                if (chapter.Documents != null && chapter.Documents.Any())
                {
                    if (keepDocuments)
                    {
                        foreach (var doc in chapter.Documents)
                        {
                            doc.ChapterId = null;
                        }
                        _context.Documents.UpdateRange(chapter.Documents);
                    }
                    else
                    {
                        var docIds = chapter.Documents.Select(d => d.Id).ToList();
                        var chunks = await _context.DocumentChunks.Where(c => docIds.Contains(c.DocumentId)).ToListAsync();
                        if (chunks.Any())
                        {
                            _context.DocumentChunks.RemoveRange(chunks);
                        }

                        foreach (var doc in chapter.Documents)
                        {
                            doc.IsDeleted = true;
                        }
                        _context.Documents.UpdateRange(chapter.Documents);
                    }
                }
                
                chapter.IsDeleted = true;
                _context.Chapters.Update(chapter);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Chapter?> GetChapterByIdAsync(int id)
        {
            return await _context.Chapters.FindAsync(id);
        }

        public async Task<(int? MaxWords, int? OverlapWords)> GetChunkSettingsAsync(int subjectId)
        {
            // Truy vấn nhẹ, không kéo theo Chapters/Documents như GetSubjectByIdAsync
            var row = await _context.Subjects
                .Where(s => s.Id == subjectId)
                .Select(s => new { s.ChunkMaxWords, s.ChunkOverlapWords })
                .FirstOrDefaultAsync();

            return row == null ? (null, null) : (row.ChunkMaxWords, row.ChunkOverlapWords);
        }

        public async Task<bool> UpdateChunkSettingsAsync(int subjectId, int? maxWords, int? overlapWords)
        {
            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null) return false;

            subject.ChunkMaxWords = maxWords;
            subject.ChunkOverlapWords = overlapWords;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
