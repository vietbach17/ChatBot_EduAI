using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO hiển thị nhật ký hoạt động tài liệu.
    /// </summary>
    public class DocumentActivityLogDto
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public int? DocumentId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
