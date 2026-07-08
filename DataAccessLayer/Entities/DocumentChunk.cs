using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace DataAccessLayer.Entities
{
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
