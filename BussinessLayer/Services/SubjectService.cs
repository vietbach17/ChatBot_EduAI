using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;

        public SubjectService(ISubjectRepository subjectRepository)
        {
            _subjectRepository = subjectRepository;
        }

        public async Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync(bool includeDeleted = false)
        {
            var subjects = await _subjectRepository.GetAllSubjectsAsync(includeDeleted);
            return subjects.Select(s => new SubjectDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                IsDeleted = s.IsDeleted,
                LecturerId = s.LecturerId,
                LecturerName = s.Lecturer?.Username,
                Chapters = (s.Chapters ?? new List<Chapter>()).Select(c => new ChapterDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    OrderIndex = c.OrderIndex,
                    Documents = (c.Documents ?? new List<Document>()).Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        Title = d.Title,
                        FileType = d.FileType,
                        FileUrl = d.FileUrl,
                        UploaderId = d.UploaderId,
                        UploaderName = d.Uploader?.Username,
                        UploadedAt = d.UploadedAt
                    }).ToList()
                }).ToList(),
                Documents = (s.Documents ?? new List<Document>())
                    .Where(d => d.ChapterId == null)
                    .Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        Title = d.Title,
                        FileType = d.FileType,
                        FileUrl = d.FileUrl,
                        UploaderId = d.UploaderId,
                        UploaderName = d.Uploader?.Username,
                        UploadedAt = d.UploadedAt
                    }).ToList()
            });
        }

        public async Task<SubjectDto?> GetSubjectByIdAsync(int id)
        {
            var s = await _subjectRepository.GetSubjectByIdAsync(id);
            if (s == null) return null;

            return new SubjectDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                IsDeleted = s.IsDeleted,
                LecturerId = s.LecturerId,
                LecturerName = s.Lecturer?.Username,
                Chapters = (s.Chapters ?? new List<Chapter>()).Select(c => new ChapterDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    OrderIndex = c.OrderIndex,
                    Documents = (c.Documents ?? new List<Document>()).Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        Title = d.Title,
                        FileType = d.FileType,
                        FileUrl = d.FileUrl,
                        UploaderId = d.UploaderId,
                        UploaderName = d.Uploader?.Username,
                        UploadedAt = d.UploadedAt
                    }).ToList()
                }).ToList(),
                Documents = (s.Documents ?? new List<Document>())
                    .Where(d => d.ChapterId == null)
                    .Select(d => new DocumentDto
                    {
                        Id = d.Id,
                        Title = d.Title,
                        FileType = d.FileType,
                        FileUrl = d.FileUrl,
                        UploaderId = d.UploaderId,
                        UploaderName = d.Uploader?.Username,
                        UploadedAt = d.UploadedAt
                    }).ToList()
            };
        }

        public async Task<IEnumerable<SubjectDto>> GetSubjectsByLecturerIdAsync(int lecturerId)
        {
            var subjects = await _subjectRepository.GetSubjectsByLecturerIdAsync(lecturerId);
            return subjects.Select(s => new SubjectDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                IsDeleted = s.IsDeleted,
                LecturerId = s.LecturerId,
                LecturerName = s.Lecturer?.Username
            });
        }

        public async Task<bool> AddSubjectAsync(string code, string name, int? lecturerId)
        {
            var subject = new Subject
            {
                Code = code,
                Name = name,
                LecturerId = lecturerId
            };
            await _subjectRepository.AddSubjectAsync(subject);
            return true;
        }

        public async Task<bool> UpdateSubjectAsync(int id, string code, string name, int? lecturerId)
        {
            var s = await _subjectRepository.GetSubjectByIdAsync(id);
            if (s == null) return false;

            s.Code = code;
            s.Name = name;
            s.LecturerId = lecturerId;
            await _subjectRepository.UpdateSubjectAsync(s);
            return true;
        }

        public async Task<bool> SoftDeleteSubjectAsync(int id)
        {
            var s = await _subjectRepository.GetSubjectByIdAsync(id);
            if (s == null) return false;

            await _subjectRepository.SoftDeleteSubjectAsync(id);
            return true;
        }

        public async Task<bool> RestoreSubjectAsync(int id)
        {
            var s = await _subjectRepository.GetSubjectByIdAsync(id);
            if (s == null) return false;

            s.IsDeleted = false;
            await _subjectRepository.UpdateSubjectAsync(s);
            return true;
        }

        public async Task<bool> AddChapterAsync(int subjectId, string title, int orderIndex)
        {
            var chapter = new Chapter
            {
                SubjectId = subjectId,
                Title = title,
                OrderIndex = orderIndex
            };
            await _subjectRepository.AddChapterAsync(chapter);
            return true;
        }

        public async Task<bool> UpdateChapterAsync(int chapterId, string title)
        {
            var chapter = await _subjectRepository.GetChapterByIdAsync(chapterId);
            if (chapter == null) return false;

            chapter.Title = title;
            await _subjectRepository.UpdateChapterAsync(chapter);
            return true;
        }

        public async Task<bool> DeleteChapterWithOptionsAsync(int chapterId, bool keepDocuments)
        {
            var chapter = await _subjectRepository.GetChapterByIdAsync(chapterId);
            if (chapter == null) return false;

            await _subjectRepository.DeleteChapterWithOptionsAsync(chapterId, keepDocuments);
            return true;
        }
    }
}
