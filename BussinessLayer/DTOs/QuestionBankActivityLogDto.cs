using System;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO hiển thị một dòng nhật ký hoạt động Ngân hàng câu hỏi (thêm/sửa/xóa/khôi phục).
    /// </summary>
    public class QuestionBankActivityLogDto
    {
        public int Id { get; set; }
        public int? QuestionBankId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string QuestionSnippet { get; set; } = string.Empty;
        public string? OldContentJson { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
