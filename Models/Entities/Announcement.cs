using System.ComponentModel.DataAnnotations;

namespace Yoklama.Models.Entities
{
    public class Announcement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required(ErrorMessage = "Başlık zorunludur")]
        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "İçerik zorunludur")]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        public Guid CreatedById { get; set; }
        
        public virtual User CreatedBy { get; set; } = null!;
        
        public bool IsActive { get; set; } = true;
        
        public int Priority { get; set; } = 1; // 1: Normal, 2: Önemli, 3: Kritik
    }
}
