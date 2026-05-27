using System.ComponentModel.DataAnnotations.Schema;

namespace Klikk.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        public string Status { get; set; }

        public ICollection<OrderItem>? OrderItems { get; set; }
    }
}