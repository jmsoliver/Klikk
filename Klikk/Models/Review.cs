using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Klikk.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Required]
        public string UserId { get; set; }

        public string? UserName { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(500)]
        public string Comment { get; set; }

        public DateTime ReviewDate { get; set; } =
            DateTime.UtcNow;
    }
}