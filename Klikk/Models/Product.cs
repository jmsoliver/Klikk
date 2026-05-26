using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klikk.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Display(Name = "Stocks")]
        public int StockQuantity { get; set; }

        [Display(Name = "Product Image")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public ICollection<Review>? Reviews { get; set; }
    }
}