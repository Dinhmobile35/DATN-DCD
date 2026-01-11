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

        public async Task<IActionResult> Index()
        {
            var cart = await GetCurrentCart();
            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            var viewModel = new CheckoutViewModel
            {
                Cart = cart,
                RecipientName = HttpContext.Session.GetString("FullName") ?? "",
                RecipientPhone = "", // Có thể lấy từ profile user
                RecipientAddress = ""
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(CheckoutViewModel model)
        {
            var cart = await GetCurrentCart();
            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId == 0)
                return RedirectToAction("Login", "Account");

            // Tạo Order
            var order = new Order
            {
                UserId = userId.Value,
                OrderCode = "DH" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                TotalAmount = cart.CartItems.Sum(i => i.Quantity * i.Product.Price),
                Status = "Mới",
                OrderDate = DateTime.Now,
                RecipientName = model.RecipientName,
                RecipientPhone = model.RecipientPhone,
                RecipientAddress = model.RecipientAddress
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Chuyển CartItem sang OrderDetail
            foreach (var item in cart.CartItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });

                // Giảm tồn kho
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                    product.Stock -= item.Quantity;
            }

            // Xóa giỏ hàng
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {order.OrderCode}";
            return RedirectToAction("OrderSuccess");
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }

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
    }
}