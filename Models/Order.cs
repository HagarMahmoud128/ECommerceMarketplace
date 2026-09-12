using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceMarketplace.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerId { get; set; } = string.Empty;
        public ApplicationUser? Customer { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        // Overall order status - kept in sync with its OrderItems' statuses
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        public string ShippingAddress { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
