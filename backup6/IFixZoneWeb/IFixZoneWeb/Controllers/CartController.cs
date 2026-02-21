using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Helpers;
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
        private readonly IViewRenderService _viewRender;

        // 🔹 BẮT BUỘC inject ViewRenderService
        public CartController(AppDbContext context, IViewRenderService viewRender)
        {
            _context = context;
            _viewRender = viewRender;
        }

        // ================= LẤY GIỎ HÀNG HIỆN TẠI =================
        private async Task<Cart?> GetCurrentCart()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return null;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

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

        // ================= XEM GIỎ HÀNG =================
        public async Task<IActionResult> Index()
        {
            var cart = await GetCurrentCart();
            if (cart == null)
                return RedirectToAction("Login", "Account");

            return View(cart);
        }

        // ================= THÊM VÀO GIỎ (AJAX) =================
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào giỏ hàng" });

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });

            if (product.Stock < quantity)
                return Json(new { success = false, message = $"Chỉ còn {product.Stock} sản phẩm trong kho" });

            var cart = await GetCurrentCart();
            if (cart == null)
                return Json(new { success = false, message = "Không lấy được giỏ hàng" });

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
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                cartCount = cart.CartItems.Sum(x => x.Quantity),
                grandTotal = cart.CartItems.Sum(x => x.Quantity * x.Product.Price)
            });
        }

        // ================= CẬP NHẬT SỐ LƯỢNG =================
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var item = await _context.CartItems
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.CartItemId == cartItemId);

            if (item == null)
                return Json(new { success = false });

            if (quantity < 1) quantity = 1;
            if (quantity > item.Product.Stock)
                return Json(new { success = false, message = "Vượt quá tồn kho" });

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            var subtotal = item.Quantity * item.Product.Price;
            var grandTotal = await _context.CartItems
                .Where(c => c.CartId == item.CartId)
                .SumAsync(c => c.Quantity * c.Product.Price);

            return Json(new { success = true, subtotal, grandTotal });
        }

        // ================= XÓA SẢN PHẨM =================
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null)
                return Json(new { success = false });

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ================= MINI CART (AJAX – CHUẨN) =================
        [HttpGet]
        public async Task<IActionResult> Mini()
        {
            var cart = await GetCurrentCart();

            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
            {
                return Json(new
                {
                    count = 0,
                    html = "<div class='text-center text-muted py-2'>Giỏ hàng trống</div>"
                });
            }

            var count = cart.CartItems.Sum(x => x.Quantity);

            // Render partial view thủ công
            var html = await RazorViewToStringRenderer.RenderViewAsync(
                this,
                "_MiniCart",
                cart
            );

            return Json(new
            {
                count,
                html
            });
        }

    }
}
