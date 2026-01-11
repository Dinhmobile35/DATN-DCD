using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy giỏ hàng hiện tại dựa trên UserId từ Session
        private async Task<Cart?> GetCurrentCart()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null || userId == 0)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem giỏ hàng";
                return null;
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId.Value,
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // Xem giỏ hàng
        public async Task<IActionResult> Index()
        {
            var cart = await GetCurrentCart();
            if (cart == null)
                return RedirectToAction("Login", "Account");

            return View(cart);
        }

        // Thêm sản phẩm vào giỏ
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var cart = await GetCurrentCart();
            if (cart == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào giỏ hàng" });

            var product = await _context.Products.FindAsync(productId);
            if (product == null || product.Stock < quantity)
                return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc hết hàng" });

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantity
                };
                cart.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm vào giỏ hàng" });
        }

        // Cập nhật số lượng
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            if (quantity <= 0) return Json(new { success = false });

            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return Json(new { success = false });

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Xóa item
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }
    }
}