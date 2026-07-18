using System;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Nhật ký tiêu thụ token AI. Mỗi bản ghi tương ứng một lượt gọi AI (chat, sinh câu hỏi...)
    /// với số token đầu vào/đầu ra thực tế do Gemini API trả về (usageMetadata).
    /// Dùng để trừ quota minh bạch và thống kê token theo người dùng / theo thời gian.
    /// </summary>
    public class TokenUsageLog
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        [MaxLength(30)]
        public string Feature { get; set; } = "chat"; // "chat", "quiz-gen", ...

        [MaxLength(80)]
        public string? Model { get; set; } // model Gemini thực tế đã trả lời

        public int PromptTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }

        /// <summary>True nếu số token là ước lượng (API không trả usageMetadata).</summary>
        public bool IsEstimated { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
