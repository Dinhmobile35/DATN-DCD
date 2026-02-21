using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IFixZoneWeb.Controllers
{
    [Authorize] // 🔐 BẮT BUỘC ĐĂNG NHẬP
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;

        public CheckoutController(AppDbContext context)
        {
            _context = context;
        }

        // ================== LẤY USER ID TỪ CLAIM ==================
        private int? GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out int id) ? id : null;
        }

        // ================== CHECKOUT PAGE ==================
        [HttpGet]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            var cart = await GetCurrentCart(userId.Value);

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            var selectedItems = cart.CartItems
                .Where(i => model.SelectedCartItemIds.Contains(i.CartItemId))
                .ToList();

            if (!selectedItems.Any())
            {
                TempData["Error"] = "Bạn chưa chọn sản phẩm nào để thanh toán!";
                return RedirectToAction("Index", "Cart");
            }

            model.Cart = cart;
            model.User = user;

            model.RecipientName = user?.FullName ?? "";
            model.RecipientPhone = user?.Phone ?? "";
            model.RecipientAddress = user?.Address ?? "";

            return View(model);
        }

        // ================== PROCESS CHECKOUT ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(CheckoutViewModel model)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            var cart = await GetCurrentCart(userId.Value);

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            var selectedItems = cart.CartItems
                .Where(i => model.SelectedCartItemIds.Contains(i.CartItemId))
                .ToList();

            if (!selectedItems.Any())
            {
                TempData["Error"] = "Không có sản phẩm nào được chọn!";
                return RedirectToAction("Index", "Cart");
            }

            // Lưu thông tin người nhận
            user.FullName = model.RecipientName;
            user.Phone = model.RecipientPhone;
            user.Address = model.RecipientAddress;
            await _context.SaveChangesAsync();

            // ===== TẠO ĐƠN HÀNG =====
            var order = new Order
            {
                UserId = userId.Value,
                OrderCode = "DH" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                TotalAmount = selectedItems.Sum(i => i.Quantity * i.Product.Price),
                Status = "Mới",
                OrderDate = DateTime.Now,
                RecipientName = model.RecipientName,
                RecipientPhone = model.RecipientPhone,
                RecipientAddress = model.RecipientAddress
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in selectedItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });

                item.Product.Stock -= item.Quantity;
            }

            _context.CartItems.RemoveRange(selectedItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {order.OrderCode}";
            return RedirectToAction("OrderSuccess");
        }

        // ================== SUCCESS ==================
        public IActionResult OrderSuccess() => View();

        // ================== GET CART ==================
        private async Task<Cart?> GetCurrentCart(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }
    }
}
