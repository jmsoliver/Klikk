using System.ComponentModel.DataAnnotations;

namespace Klikk.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        public DateTime AddedDate { get; set; } =
            DateTime.Now;
    }
}