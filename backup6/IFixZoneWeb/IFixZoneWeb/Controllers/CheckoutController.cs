using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;

        public CheckoutController(AppDbContext context)
        {
            _context = context;
        }

        // ================== CHECKOUT PAGE ==================
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            var cart = await GetCurrentCart();

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            // Chỉ lấy item được chọn
            var selectedItems = cart.CartItems
                .Where(i => model.SelectedCartItemIds.Contains(i.CartItemId))
                .ToList();

            if (!selectedItems.Any())
            {
                TempData["Error"] = "Bạn chưa chọn sản phẩm nào để thanh toán!";
                return RedirectToAction("Index", "Cart");
            }

            // Gán lại để view dùng
            model.Cart = cart;
            model.User = user;

            // 🔥 AUTO LOAD THÔNG TIN NGƯỜI DÙNG
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
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            var cart = await GetCurrentCart();

            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            var selectedItems = cart.CartItems
                .Where(i => model.SelectedCartItemIds.Contains(i.CartItemId))
                .ToList();

            if (!selectedItems.Any())
            {
                TempData["Error"] = "Không có sản phẩm nào được chọn để thanh toán!";
                return RedirectToAction("Index", "Cart");
            }

            // 🔥 LƯU THÔNG TIN NGƯỜI DÙNG CHO LẦN SAU
            if (user != null)
            {
                user.FullName = model.RecipientName;
                user.Phone = model.RecipientPhone;
                user.Address = model.RecipientAddress;
                await _context.SaveChangesAsync();
            }

            // ================== TẠO ORDER ==================
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

            // ================== ORDER DETAILS ==================
            foreach (var item in selectedItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });

                // Trừ tồn kho
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                    product.Stock -= item.Quantity;
            }

            // Xóa item đã thanh toán khỏi giỏ
            _context.CartItems.RemoveRange(selectedItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {order.OrderCode}";
            return RedirectToAction("OrderSuccess");
        }

        // ================== SUCCESS ==================
        public IActionResult OrderSuccess()
        {
            return View();
        }

        // ================== GET CART ==================
        private async Task<Cart?> GetCurrentCart()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
                return null;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
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
    }
}
