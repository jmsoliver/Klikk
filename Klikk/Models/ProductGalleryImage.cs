using System.ComponentModel.DataAnnotations;

namespace Klikk.Models
{
    public class ProductGalleryImage
    {
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        // ========================================
        // RELATIONSHIP
        // ========================================

        public int ProductId { get; set; }

        public Product? Product { get; set; }
    }
}