using ECommerceMarketplace.Data;
using ECommerceMarketplace.Models;
using ECommerceMarketplace.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMarketplace.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var customers = await _userManager.GetUsersInRoleAsync("Customer");
            var sellers = await _userManager.GetUsersInRoleAsync("Seller");

            var vm = new AdminDashboardViewModel
            {
                TotalCustomers = customers.Count,
                TotalSellers = sellers.Count,
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                PendingSellerRequests = await _context.SellerRequests.CountAsync(r => r.Status == SellerRequestStatus.Pending)
            };

            return View(vm);
        }

        // ---------- Users ----------
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.OrderBy(u => u.FullName).ToListAsync();
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var u in users)
                userRoles[u.Id] = await _userManager.GetRolesAsync(u);

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSuspend(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsSuspended = !user.IsSuspended;
                await _userManager.UpdateAsync(user);
                TempData["Message"] = user.IsSuspended ? "User suspended." : "User activated.";
            }

            return RedirectToAction(nameof(Users));
        }

        // ---------- Seller Requests ----------
        public async Task<IActionResult> SellerRequests()
        {
            var requests = await _context.SellerRequests
                .Include(r => r.User)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSeller(int id)
        {
            var request = await _context.SellerRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);
            if (request != null && request.Status == SellerRequestStatus.Pending)
            {
                request.Status = SellerRequestStatus.Approved;
                request.ReviewedDate = DateTime.UtcNow;

                if (!await _userManager.IsInRoleAsync(request.User!, "Seller"))
                    await _userManager.AddToRoleAsync(request.User!, "Seller");

                await _context.SaveChangesAsync();
                TempData["Message"] = "Seller approved.";
            }

            return RedirectToAction(nameof(SellerRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSeller(int id)
        {
            var request = await _context.SellerRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request != null && request.Status == SellerRequestStatus.Pending)
            {
                request.Status = SellerRequestStatus.Rejected;
                request.ReviewedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Seller request rejected.";
            }

            return RedirectToAction(nameof(SellerRequests));
        }

        // ---------- Products ----------
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsRemovedByAdmin = true;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Product removed.";
            }

            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsRemovedByAdmin = false;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Product restored.";
            }

            return RedirectToAction(nameof(Products));
        }

        // ---------- Categories ----------
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }

        public IActionResult CreateCategory() => View("CategoryForm", new Category());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category model)
        {
            if (!ModelState.IsValid) return View("CategoryForm", model);

            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Category created.";
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View("CategoryForm", category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category model)
        {
            if (!ModelState.IsValid) return View("CategoryForm", model);

            var category = await _context.Categories.FindAsync(model.Id);
            if (category == null) return NotFound();

            category.Name = model.Name;
            category.Description = model.Description;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Category updated.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                bool inUse = await _context.Products.AnyAsync(p => p.CategoryId == id);
                if (inUse)
                {
                    TempData["Error"] = "Cannot delete a category that still has products assigned to it.";
                }
                else
                {
                    _context.Categories.Remove(category);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Category deleted.";
                }
            }

            return RedirectToAction(nameof(Categories));
        }

        // ---------- Orders ----------
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Seller)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}
