using System.ComponentModel.DataAnnotations;
using ECommerceMarketplace.Models;

namespace ECommerceMarketplace.ViewModels
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? SortOrder { get; set; }
    }

    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        [Display(Name = "Available Quantity")]
        public int AvailableQuantity { get; set; }

        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public List<Category> Categories { get; set; } = new();
    }

    public class ReviewViewModel
    {
        public int ProductId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [Required, StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }
}
