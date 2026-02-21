using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFixZoneWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        public CartController(AppDbContext context) => _context = context;

        private async Task<Cart?> GetCurrentCart()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return null;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId.Value, CreatedAt = DateTime.Now };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // XEM GIỎ HÀNG
        public async Task<IActionResult> Index()
        {
            var cart = await GetCurrentCart();
            if (cart == null) return RedirectToAction("Login", "Account");
            return View(cart);
        }

        // THÊM VÀO GIỎ
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var cart = await GetCurrentCart();
            if (cart == null)
                return Json(new { success = false, message = "Bạn cần đăng nhập trước" });

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });

            var item = cart.CartItems.FirstOrDefault(x => x.ProductId == productId);

            if (item == null)
            {
                item = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantity
                };
                cart.CartItems.Add(item);
            }
            else
            {
                item.Quantity += quantity;
                if (item.Quantity < 1) item.Quantity = 1;
            }

            await _context.SaveChangesAsync();

            var total = cart.CartItems.Sum(x => x.Quantity * x.Product.Price);

            return Json(new
            {
                success = true,
                message = "Đã thêm sản phẩm vào giỏ hàng",
                grandTotal = total
            });
        }

        // CẬP NHẬT SỐ LƯỢNG
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var item = await _context.CartItems
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.CartItemId == cartItemId);

            if (item == null)
                return Json(new { success = false });

            if (quantity < 1) quantity = 1;

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            var subtotal = item.Quantity * item.Product.Price;

            return Json(new
            {
                success = true,
                subtotal,
                grandTotal = await _context.CartItems
                    .Where(c => c.CartId == item.CartId)
                    .SumAsync(c => c.Quantity * c.Product.Price)
            });
        }

        // XOÁ SẢN PHẨM
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return Json(new { success = false });

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
