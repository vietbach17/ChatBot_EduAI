using DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.IRepositories
{
    /// <summary>
    /// Giao diện Repository truy vấn nhật ký hoạt động Tài liệu.
    /// </summary>
    public interface IDocumentActivityLogRepository
    {
        Task AddLogAsync(DocumentActivityLog log);
        Task<IEnumerable<DocumentActivityLog>> GetLogsBySubjectIdAsync(int subjectId);
    }
}
