using ECommerceMarketplace.Data;
using ECommerceMarketplace.Models;
using ECommerceMarketplace.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMarketplace.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Browse / search / filter by category / sort by price
        public async Task<IActionResult> Index(string? search, int? categoryId, string? sort)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Reviews)
                .Where(p => !p.IsRemovedByAdmin)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderByDescending(p => p.CreatedDate)
            };

            var vm = new ProductListViewModel
            {
                Products = await query.ToListAsync(),
                Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                SearchTerm = search,
                CategoryId = categoryId,
                SortOrder = sort
            };

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Reviews).ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsRemovedByAdmin);

            if (product == null) return NotFound();

            // A customer can review only if they've bought and received this product,
            // and haven't reviewed it already.
            bool canReview = false;
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Customer"))
            {
                var user = await _userManager.GetUserAsync(User);
                bool purchased = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .AnyAsync(oi => oi.ProductId == id
                        && oi.Order!.CustomerId == user!.Id
                        && oi.Order.Status == OrderStatus.Delivered);

                bool alreadyReviewed = await _context.Reviews
                    .AnyAsync(r => r.ProductId == id && r.UserId == user!.Id);

                canReview = purchased && !alreadyReviewed;
            }
            ViewBag.CanReview = canReview;

            return View(product);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(ReviewViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            bool purchased = await _context.OrderItems
                .Include(oi => oi.Order)
                .AnyAsync(oi => oi.ProductId == model.ProductId
                    && oi.Order!.CustomerId == user!.Id
                    && oi.Order.Status == OrderStatus.Delivered);

            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.ProductId == model.ProductId && r.UserId == user!.Id);

            if (purchased && !alreadyReviewed && ModelState.IsValid)
            {
                _context.Reviews.Add(new Review
                {
                    ProductId = model.ProductId,
                    UserId = user!.Id,
                    Rating = model.Rating,
                    Comment = model.Comment
                });
                await _context.SaveChangesAsync();
                TempData["Message"] = "Thanks for your review!";
            }
            else
            {
                TempData["Error"] = "You can only review products you have purchased and received.";
            }

            return RedirectToAction(nameof(Details), new { id = model.ProductId });
        }
    }
}
