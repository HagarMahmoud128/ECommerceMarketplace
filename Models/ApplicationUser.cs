using Microsoft.AspNetCore.Identity;

namespace ECommerceMarketplace.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Admin can suspend a user; suspended users cannot log in.
        public bool IsSuspended { get; set; } = false;

        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
