using BussinessLayer.IServices;
using BussinessLayer.IGateways;
using BussinessLayer.Gateways;
using BussinessLayer.DTOs;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLayer.Services
{
    /// <summary>
    /// Dịch vụ ghi nhật ký hoạt động Tài liệu. Lưu lại lịch sử các thao tác: Tải lên, Xóa, Di chuyển tài liệu.
    /// </summary>
    public class DocumentActivityLogService : IDocumentActivityLogService
    {
        private readonly IDocumentActivityLogRepository _repository;

        public DocumentActivityLogService(IDocumentActivityLogRepository repository)
        {
            _repository = repository;
        }

        public async Task LogActivityAsync(int subjectId, int? documentId, string documentTitle, int userId, string action)
        {
            var log = new DocumentActivityLog
            {
                SubjectId = subjectId,
                DocumentId = documentId,
                DocumentTitle = documentTitle,
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow
            };
            await _repository.AddLogAsync(log);
        }

        public async Task<IEnumerable<DocumentActivityLogDto>> GetLogsBySubjectIdAsync(int subjectId)
        {
            var logs = await _repository.GetLogsBySubjectIdAsync(subjectId);
            return logs.Select(l => new DocumentActivityLogDto
            {
                Id = l.Id,
                SubjectId = l.SubjectId,
                DocumentId = l.DocumentId,
                DocumentTitle = l.DocumentTitle,
                UserId = l.UserId,
                UserName = l.User?.Username ?? "Unknown",
                Action = l.Action,
                Timestamp = l.Timestamp
            });
        }
    }
}

