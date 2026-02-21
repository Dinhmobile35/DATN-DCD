using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers
{
    [Authorize] // 🔐 BẮT BUỘC ĐĂNG NHẬP
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IViewRenderService _viewRender;

        public CartController(AppDbContext context, IViewRenderService viewRender)
        {
            _context = context;
            _viewRender = viewRender;
        }

        // =====================================================
        // 🔑 LẤY USER ID TỪ CLAIM (COOKIE AUTH)
        // =====================================================
        private int? GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out int id) ? id : null;
        }

        // =====================================================
        // 🛍️ LẤY GIỎ HÀNG HIỆN TẠI (AUTO CREATE)
        // =====================================================
        private async Task<Cart?> GetCurrentCart()
        {
            var userId = GetUserId();
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

        // =====================================================
        // 👀 XEM GIỎ HÀNG
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var cart = await GetCurrentCart();
            return View(cart);
        }

        // =====================================================
        // ➕ THÊM VÀO GIỎ (AJAX)
        // ✔ CHECK LOGIN
        // ✔ CHECK PRODUCT ACTIVE
        // ✔ CHECK CỘNG DỒN TỒN KHO
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = GetUserId();

            // 🔐 CHƯA ĐĂNG NHẬP
            if (!userId.HasValue)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    loginUrl = Url.Action(
                        "Login",
                        "Account",
                        new { returnUrl = Request.Headers["Referer"].ToString() }
                    )
                });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null || product.Status != "Active")
            {
                return Json(new
                {
                    success = false,
                    message = "Sản phẩm không tồn tại"
                });
            }

            var cart = await GetCurrentCart();
            if (cart == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không lấy được giỏ hàng"
                });
            }

            var item = cart.CartItems.FirstOrDefault(x => x.ProductId == productId);
            var existingQty = item?.Quantity ?? 0;

            // 🔒 CHECK CỘNG DỒN VƯỢT TỒN KHO
            if (existingQty + quantity > product.Stock)
            {
                return Json(new
                {
                    success = false,
                    message = $"Chỉ còn {product.Stock} sản phẩm trong kho"
                });
            }

            if (item == null)
            {
                cart.CartItems.Add(new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity += quantity;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                quantity = cart.CartItems.Sum(x => x.Quantity),
                itemCount = cart.CartItems.Count
            });
        }


        // =====================================================
        // 🔄 CẬP NHẬT SỐ LƯỢNG
        // ✔ CHECK USER OWNERSHIP
        // ✔ CHECK ACTIVE
        // ✔ CHECK TỒN KHO
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Json(new { success = false });

            var item = await _context.CartItems
                .Include(x => x.Product)
                .Include(x => x.Cart)
                .FirstOrDefaultAsync(x =>
                    x.CartItemId == cartItemId &&
                    x.Cart.UserId == userId);

            if (item == null || item.Product == null || item.Product.Status != "Active")
                return Json(new { success = false });

            if (quantity < 1) quantity = 1;
            if (quantity > item.Product.Stock)
            {
                return Json(new
                {
                    success = false,
                    message = $"Chỉ còn {item.Product.Stock} sản phẩm"
                });
            }

            item.Quantity = quantity;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                subtotal = item.Quantity * item.Product.Price
            });
        }

        // =====================================================
        // ❌ XÓA SẢN PHẨM
        // ✔ CHECK USER OWNERSHIP
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Json(new { success = false });

            var item = await _context.CartItems
                .Include(x => x.Cart)
                .FirstOrDefaultAsync(x =>
                    x.CartItemId == cartItemId &&
                    x.Cart.UserId == userId);

            if (item == null)
                return Json(new { success = false });

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // =====================================================
        // 🧹 CLEAR CART
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var cart = await GetCurrentCart();
            if (cart == null)
                return Json(new { success = false });

            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // =====================================================
        // 🧺 MINI CART (AJAX)
        // ✔ TRẢ: quantity + itemCount + html
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Mini()
        {
            var cart = await GetCurrentCart();

            if (cart == null || !cart.CartItems.Any())
            {
                return Json(new
                {
                    quantity = 0,
                    itemCount = 0,
                    html = "<div class='text-center text-muted py-2'>Giỏ hàng trống</div>"
                });
            }

            var html = await RazorViewToStringRenderer.RenderViewAsync(
                this,
                "_MiniCart",
                cart
            );

            return Json(new
            {
                quantity = cart.CartItems.Sum(x => x.Quantity),
                itemCount = cart.CartItems.Count,
                html
            });
        }
    }
}
