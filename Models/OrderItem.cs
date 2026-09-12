using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceMarketplace.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        // Denormalized so a seller can query/update only the items that belong to them,
        // even though a single order can contain products from multiple sellers.
        public string SellerId { get; set; } = string.Empty;
        public ApplicationUser? Seller { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [NotMapped]
        public decimal Subtotal => Quantity * UnitPrice;
    }
}
