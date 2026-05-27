using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Klikk.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public int CategoryId { get; set; }

        public string? ExistingImageUrl { get; set; }

        public IFormFile? ImageFile { get; set; }

        public List<IFormFile>? GalleryImages { get; set; }

        public List<ProductGalleryImage>? ExistingGalleryImages { get; set; }

    }
}