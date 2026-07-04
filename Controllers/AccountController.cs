using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineGroceryShop.Data;
using OnlineGroceryShop.Models;

namespace OnlineGroceryShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string phoneNumber, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.PasswordHash == password);

            if (user != null)
            {
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("Username", user.Name);
                HttpContext.Session.SetInt32("UserId", user.Id);

                // 🔹 Redirect based on role
                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (user.Role == "Seller")
                {
                    return RedirectToAction("Index", "Admin"); // Seller can manage products too
                }
                else
                {
                    return RedirectToAction("Index", "Home"); // Normal user
                }
            }

            ViewBag.Error = "Invalid phone number or password";
            return View();
        }

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View(user);
                }

                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == user.PhoneNumber);
                if (phoneExists)
                {
                    ModelState.AddModelError("PhoneNumber", "An account with this phone number already exists.");
                    return View(user);
                }

                // 🔹 Default role
                if (string.IsNullOrEmpty(user.Role))
                {
                    user.Role = "Seller";
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Login));
            }

            return View(user);
        }

        public async Task<IActionResult> OrderHistory()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var orders = await _context.Orders
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p!.Category)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId.Value);

            if (order == null) return NotFound();

            return View(order);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}