using ECommerceMarketplace.Data;
using ECommerceMarketplace.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMarketplace.Controllers
{
    [Authorize(Roles = "Customer")]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _context.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == user!.Id)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            var exists = await _context.WishlistItems
                .AnyAsync(w => w.UserId == user!.Id && w.ProductId == productId);

            if (!exists)
            {
                _context.WishlistItems.Add(new WishlistItem { UserId = user!.Id, ProductId = productId });
                await _context.SaveChangesAsync();
                TempData["Message"] = "Added to wishlist.";
            }

            return RedirectToAction("Details", "Products", new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.Id == id && w.UserId == user!.Id);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
