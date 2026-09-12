using ECommerceMarketplace.Data;
using ECommerceMarketplace.Models;
using ECommerceMarketplace.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMarketplace.Controllers
{
    // Only users in the Seller role (i.e. approved by an admin) reach this controller.
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SellerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            var totalProducts = await _context.Products.CountAsync(p => p.SellerId == user!.Id);
            var orderItems = await _context.OrderItems
                .Where(oi => oi.SellerId == user!.Id)
                .ToListAsync();

            var vm = new SellerDashboardViewModel
            {
                TotalProducts = totalProducts,
                TotalOrders = orderItems.Select(oi => oi.OrderId).Distinct().Count(),
                TotalSales = orderItems.Where(oi => oi.Status == OrderStatus.Delivered).Sum(oi => oi.Subtotal),
                PendingOrderItems = orderItems.Count(oi => oi.Status == OrderStatus.Pending)
            };

            return View(vm);
        }

        public async Task<IActionResult> Products()
        {
            var user = await _userManager.GetUserAsync(User);
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.SellerId == user!.Id)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> CreateProduct()
        {
            var vm = new ProductFormViewModel
            {
                Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync()
            };
            return View("ProductForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                return View("ProductForm", model);
            }

            var user = await _userManager.GetUserAsync(User);
            _context.Products.Add(new Product
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                AvailableQuantity = model.AvailableQuantity,
                ImageUrl = model.ImageUrl,
                CategoryId = model.CategoryId,
                SellerId = user!.Id
            });
            await _context.SaveChangesAsync();

            TempData["Message"] = "Product created.";
            return RedirectToAction(nameof(Products));
        }

        public async Task<IActionResult> EditProduct(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == user!.Id);

            if (product == null) return NotFound(); // sellers can only manage their own products

            var vm = new ProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                AvailableQuantity = product.AvailableQuantity,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId,
                Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync()
            };
            return View("ProductForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductFormViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == model.Id && p.SellerId == user!.Id);

            if (product == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                return View("ProductForm", model);
            }

            product.Name = model.Name;
            product.Description = model.Description;
            product.Price = model.Price;
            product.AvailableQuantity = model.AvailableQuantity;
            product.ImageUrl = model.ImageUrl;
            product.CategoryId = model.CategoryId;

            await _context.SaveChangesAsync();
            TempData["Message"] = "Product updated.";
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == user!.Id);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Product deleted.";
            }

            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == user!.Id);

            if (product != null && quantity >= 0)
            {
                product.AvailableQuantity = quantity;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Quantity updated.";
            }

            return RedirectToAction(nameof(Products));
        }

        // Orders containing this seller's products (only their own line items)
        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.SellerId == user!.Id)
                .OrderByDescending(oi => oi.Order!.OrderDate)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderItemId, OrderStatus status)
        {
            var user = await _userManager.GetUserAsync(User);
            var item = await _context.OrderItems
                .Include(oi => oi.Order).ThenInclude(o => o!.OrderItems)
                .FirstOrDefaultAsync(oi => oi.Id == orderItemId && oi.SellerId == user!.Id);

            if (item != null && item.Status != OrderStatus.Cancelled)
            {
                item.Status = status;

                // Keep the overall order status roughly in sync with its items
                var order = item.Order!;
                if (order.OrderItems.All(i => i.Status == OrderStatus.Delivered))
                    order.Status = OrderStatus.Delivered;
                else if (order.OrderItems.Any(i => i.Status == OrderStatus.Shipped))
                    order.Status = OrderStatus.Shipped;
                else if (order.OrderItems.All(i => i.Status is OrderStatus.Confirmed or OrderStatus.Shipped or OrderStatus.Delivered))
                    order.Status = OrderStatus.Confirmed;

                await _context.SaveChangesAsync();
                TempData["Message"] = "Order status updated.";
            }

            return RedirectToAction(nameof(Orders));
        }
    }
}
