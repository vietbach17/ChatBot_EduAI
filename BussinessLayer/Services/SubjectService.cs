using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ Quản lý Môn học. CRUD Môn học, Chương (Chapter), phân công Giảng viên, và đồng bộ real-time qua SignalR.
    /// </summary>
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;
        private readonly IChunkSettingsService _chunkSettingsService;

        public SubjectService(ISubjectRepository subjectRepository, IChunkSettingsService chunkSettingsService)
        {
            _subjectRepository = subjectRepository;
            _chunkSettingsService = chunkSettingsService;
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

        public async Task<SubjectChunkSettingsDto> GetChunkSettingsAsync(int subjectId)
        {
            var policy = _chunkSettingsService.GetPolicy();
            var (maxWords, overlapWords) = await _subjectRepository.GetChunkSettingsAsync(subjectId);
            // Admin tắt quyền → môn hiển thị và chạy theo template, dù cấu hình riêng vẫn còn lưu
            var useCustom = policy.AllowLecturerOverride && maxWords.HasValue && overlapWords.HasValue;

            return new SubjectChunkSettingsDto
            {
                SubjectId = subjectId,
                UseCustom = useCustom,
                MaxWords = useCustom ? maxWords!.Value : policy.MaxWords,
                OverlapWords = useCustom ? overlapWords!.Value : policy.OverlapWords,
                Policy = policy
            };
        }

        public async Task<(bool Success, string? Error)> UpdateChunkSettingsAsync(int subjectId, bool useCustom, int maxWords, int overlapWords)
        {
            if (!useCustom)
            {
                var cleared = await _subjectRepository.UpdateChunkSettingsAsync(subjectId, null, null);
                return cleared ? (true, null) : (false, "Không tìm thấy môn học.");
            }

            var error = _chunkSettingsService.ValidateLecturerSettings(new ChunkSettingsDto
            {
                MaxWords = maxWords,
                OverlapWords = overlapWords
            });
            if (error != null) return (false, error);

            var updated = await _subjectRepository.UpdateChunkSettingsAsync(subjectId, maxWords, overlapWords);
            return updated ? (true, null) : (false, "Không tìm thấy môn học.");
        }
    }
}

