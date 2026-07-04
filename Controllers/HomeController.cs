using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineGroceryShop.Data;
using OnlineGroceryShop.Models;

namespace OnlineGroceryShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Home Page - Show all products
        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            // Load products with categories
            var products = _context.Products
                                   .Include(p => p.Category)
                                   .AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString));
            }

            // Category filter
            if (categoryId != null)
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }

            // Send categories to view
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Return products to view
            var productList = await products.ToListAsync();

            return View(productList);
        }

        // Product details page
        public async Task<IActionResult> Details(int id)
        {
            if (id == 0)
                return NotFound();

            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Service()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}