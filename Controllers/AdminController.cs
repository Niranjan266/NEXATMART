using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineGroceryShop.Data;
using OnlineGroceryShop.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OnlineGroceryShop.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

public AdminController(ApplicationDbContext context)
{
    _context = context;
}

        // ADMIN PRODUCT LIST
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Index", "Home");

            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();

            return View(products);
        }

        // ADD PRODUCT PAGE
        public IActionResult AddProduct()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Index", "Home");

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // ADD PRODUCT SAVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product product)
        {
            ModelState.Remove("Category");
            if (ModelState.IsValid)
            {
                try 
                {
                    product.Description ??= "No description available.";
                    product.ImagePath ??= "/images/no-image.png";

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Database Error: " + (ex.InnerException?.Message ?? ex.Message));
                }
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // EDIT PRODUCT PAGE
        public async Task<IActionResult> EditProduct(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Index", "Home");

            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);

            return View(product);
        }

        // EDIT PRODUCT SAVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product)
        {
            if (id != product.Id)
                return NotFound();

            ModelState.Remove("Category");
            if (ModelState.IsValid)
            {
                try
                {
                    product.Description ??= "No description available.";
                    product.ImagePath ??= "/images/no-image.png";

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == product.Id))
                        return NotFound();
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Database Error: " + (ex.InnerException?.Message ?? ex.Message));
                    ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
                    return View(product);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // DELETE PRODUCT OTP SEND
        [HttpPost]
        public async Task<IActionResult> SendDeleteOtp()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Json(new { success = false, message = "Unauthorized" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var adminUser = await _context.Users.FindAsync(userId);

            if (adminUser == null)
                return Json(new { success = false, message = "Admin not found" });

            // Generate 6 digit OTP
            Random rand = new Random();
            string otp = rand.Next(100000, 999999).ToString();

            // Store in session
            HttpContext.Session.SetString("DeleteOtp", otp);

            string targetEmail = "niranjanlithikasan937@gmail.com";

            try
            {
                var smtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    // IMPORTANT: You must replace "YOUR_GMAIL_APP_PASSWORD_HERE" with a real App Password from your Google Account
                    Credentials = new System.Net.NetworkCredential("niranjanlithikasan937@gmail.com", "lpjl mgep uuep jdjs"),
                    EnableSsl = true,
                };

                var mailMessage = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress("niranjanlithikasan937@gmail.com"),
                    Subject = "Nexamart Security: Product Deletion OTP",
                    Body = $"Hello Admin,\n\nYour OTP for securely deleting a product is: {otp}\n\nDo not share this code with anyone.",
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(targetEmail);

                // Send the email asynchronously
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to send email. Ensure you have entered your Google App Password. Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }

            return Json(new { success = true, email = targetEmail });
        }

        // DELETE PRODUCT
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id, string otp)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Index", "Home");

            var sessionOtp = HttpContext.Session.GetString("DeleteOtp");

            if (string.IsNullOrEmpty(sessionOtp) || sessionOtp != otp)
            {
                TempData["Error"] = "Incorrect OTP! The product was not deleted.";
                return RedirectToAction(nameof(Index));
            }

            // Clear OTP
            HttpContext.Session.Remove("DeleteOtp");

            var product = await _context.Products.FindAsync(id);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product successfully removed from inventory.";
            }

            return RedirectToAction(nameof(Index));
        }

        // CATEGORY LIST
        public async Task<IActionResult> Categories()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Index", "Home");

            return View(await _context.Categories.ToListAsync());
        }

        // ADD CATEGORY PAGE
        public IActionResult AddCategory()
        {
            return View();
        }

        // ADD CATEGORY SAVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Categories));
            }

            return View(category);
        }

        // DELETE CATEGORY
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Index", "Home");

            var category = await _context.Categories.FindAsync(id);

            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Categories));
        }

        // VIEW ORDERS
        public async Task<IActionResult> Orders()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Index", "Home");

            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // UPDATE ORDER STATUS
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Index", "Home");

            var order = await _context.Orders.FindAsync(id);

            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Orders));
        }

        // SALES REPORT
        public async Task<IActionResult> SalesReport()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return RedirectToAction("Index", "Home");

            var orders = await _context.Orders
                .Where(o => o.Status == "Delivered")
                .ToListAsync();

            var reports = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalSales = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderByDescending(r => r.Date)
                .ToList();

            return View(reports);
        }
    }
}