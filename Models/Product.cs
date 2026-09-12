using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceMarketplace.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int AvailableQuantity { get; set; }

        public string? ImageUrl { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // The seller who owns this product
        public string SellerId { get; set; } = string.Empty;
        public ApplicationUser? Seller { get; set; }

        // Admin can remove inappropriate products without deleting them permanently
        public bool IsRemovedByAdmin { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        [NotMapped]
        public double AverageRating => Reviews.Any() ? Reviews.Average(r => r.Rating) : 0;
    }
}
