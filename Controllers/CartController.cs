using ECommerceMarketplace.Data;
using ECommerceMarketplace.Models;
using ECommerceMarketplace.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMarketplace.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var product = await _context.Products.FindAsync(productId);
            if (product == null || product.IsRemovedByAdmin) return NotFound();

            if (quantity < 1) quantity = 1;

            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ProductId == productId);

            if (existing != null)
            {
                existing.Quantity = Math.Min(existing.Quantity + quantity, product.AvailableQuantity);
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = user.Id,
                    ProductId = productId,
                    Quantity = Math.Min(quantity, Math.Max(product.AvailableQuantity, 0))
                });
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Product added to cart.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var item = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == user.Id);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                }
                else
                {
                    // Prevent ordering more than available stock
                    item.Quantity = Math.Min(quantity, item.Product!.AvailableQuantity);
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == user.Id);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (!items.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new CheckoutViewModel
            {
                TotalPrice = items.Sum(i => i.Quantity * i.Product!.Price),
                ShippingAddress = user.Address
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (!items.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.ShippingAddress))
            {
                TempData["Error"] = "Please provide a shipping address.";
                return RedirectToAction(nameof(Checkout));
            }

            // Re-validate stock to prevent overselling (race conditions / stale cart)
            foreach (var item in items)
            {
                if (item.Product == null || item.Quantity > item.Product.AvailableQuantity)
                {
                    TempData["Error"] = $"Not enough stock for '{item.Product?.Name ?? "Product"}'. Only {item.Product?.AvailableQuantity ?? 0} left.";
                    return RedirectToAction(nameof(Index));
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    CustomerId = user.Id,
                    ShippingAddress = model.ShippingAddress,
                    Status = OrderStatus.Pending,
                    TotalPrice = items.Sum(i => i.Quantity * i.Product!.Price)
                };

                foreach (var item in items)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        SellerId = item.Product!.SellerId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price,
                        Status = OrderStatus.Pending
                    });

                    item.Product.AvailableQuantity -= item.Quantity;
                }

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(items);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Message"] = "Order placed successfully!";
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "An error occurred while processing your order. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}