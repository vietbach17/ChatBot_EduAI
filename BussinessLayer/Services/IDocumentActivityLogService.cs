using BussinessLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLayer.Services
{
    public interface IDocumentActivityLogService
    {
        Task LogActivityAsync(int subjectId, int? documentId, string documentTitle, int userId, string action);
        Task<IEnumerable<DocumentActivityLogDto>> GetLogsBySubjectIdAsync(int subjectId);
    }
}
