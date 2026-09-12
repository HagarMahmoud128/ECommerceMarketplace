namespace ECommerceMarketplace.Models
{
    public class SellerRequest
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public SellerRequestStatus Status { get; set; } = SellerRequestStatus.Pending;
        public DateTime? ReviewedDate { get; set; }

        public string? BusinessName { get; set; }
    }
}
