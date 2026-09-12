using System.ComponentModel.DataAnnotations;

namespace ECommerceMarketplace.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Shipping address is required.")]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int PendingSellerRequests { get; set; }
    }

    public class SellerDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public int PendingOrderItems { get; set; }
    }
}
