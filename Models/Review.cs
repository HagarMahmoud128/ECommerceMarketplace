using System.ComponentModel.DataAnnotations;

namespace ECommerceMarketplace.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required, StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
