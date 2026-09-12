using ECommerceMarketplace.Data;
using ECommerceMarketplace.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMarketplace.Controllers
{
    [Authorize(Roles = "Customer")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == user!.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Seller)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == user!.Id);

            if (order == null) return NotFound();
            return View(order);
        }

        // Customers can cancel while the order hasn't shipped yet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == user!.Id);

            if (order == null) return NotFound();

            if (order.Status is OrderStatus.Pending or OrderStatus.Confirmed)
            {
                order.Status = OrderStatus.Cancelled;
                foreach (var item in order.OrderItems)
                {
                    item.Status = OrderStatus.Cancelled;
                    item.Product!.AvailableQuantity += item.Quantity; // restock
                }
                await _context.SaveChangesAsync();
                TempData["Message"] = "Order cancelled.";
            }
            else
            {
                TempData["Error"] = "This order can no longer be cancelled.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
