using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities
{
    public class Chapter
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        public Subject? Subject { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public int OrderIndex { get; set; } = 0;
        
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        
        public bool IsDeleted { get; set; } = false;
    }
}
