using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Thực thể lưu trữ một đoạn nhỏ (chunk) của Tài liệu sau khi được chia nhỏ.
    /// Mỗi chunk chứa nội dung văn bản (Content) và vector Embedding 768 chiều (pgvector)
    /// để phục vụ tìm kiếm ngữ nghĩa (Semantic Search) khi Sinh viên đặt câu hỏi cho AI.
    /// </summary>
    public class DocumentChunk
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }
        
        [ForeignKey("DocumentId")]
        public Document Document { get; set; } = null!;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Column(TypeName = "vector(768)")]
        public Vector? Embedding { get; set; }
        
        public int OrderIndex { get; set; }
    }
}
