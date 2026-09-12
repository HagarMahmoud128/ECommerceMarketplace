using ECommerceMarketplace.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMarketplace.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featured = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Where(p => !p.IsRemovedByAdmin)
                .OrderByDescending(p => p.CreatedDate)
                .Take(8)
                .ToListAsync();

            return View(featured);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
