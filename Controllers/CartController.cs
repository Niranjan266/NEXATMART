using Microsoft.AspNetCore.Mvc;
using OnlineGroceryShop.Data;
using OnlineGroceryShop.Models;
using System.Text.Json;

namespace OnlineGroceryShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        public IActionResult AddToCart(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = _context.Products.Find(productId);
            if (product != null)
            {
                var cart = GetCart();
                var item = cart.FirstOrDefault(i => i.ProductId == productId);
                if (item == null)
                {
                    cart.Add(new OrderDetail { ProductId = productId, Product = product, Quantity = 1, UnitPrice = product.Price });
                }
                else
                {
                    item.Quantity++;
                }
                SaveCart(cart);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult AddToCartAjax(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var product = _context.Products.Find(productId);
            if (product != null)
            {
                var cart = GetCart();
                var item = cart.FirstOrDefault(i => i.ProductId == productId);
                if (item == null)
                {
                    cart.Add(new OrderDetail { ProductId = productId, Product = product, Quantity = 1, UnitPrice = product.Price });
                }
                else
                {
                    item.Quantity++;
                }
                SaveCart(cart);
                return Json(new { success = true, cartCount = cart.Sum(i => i.Quantity) });
            }
            return Json(new { success = false, message = "Product not found" });
        }

        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            if (item != null) cart.Remove(item);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        private List<OrderDetail> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return cartJson == null ? new List<OrderDetail>() : JsonSerializer.Deserialize<List<OrderDetail>>(cartJson) ?? new List<OrderDetail>();
        }

        private void SaveCart(List<OrderDetail> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        public IActionResult Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index", "Home");

            var order = new Order
            {
                UserId = userId.Value,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(i => i.UnitPrice * i.Quantity),
                Status = "Processing",
                OrderDetails = cart.Select(i => new OrderDetail { ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList()
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            HttpContext.Session.Remove("Cart");
            return View("OrderSuccess", order.Id);
        }
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = GetCart();
            return Json(cart.Sum(i => i.Quantity));
        }
    }
}
