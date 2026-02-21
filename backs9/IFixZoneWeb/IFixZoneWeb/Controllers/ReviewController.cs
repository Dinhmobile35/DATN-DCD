using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IFixZoneWeb.Controllers
{
    [Authorize] // 🔐 BẮT BUỘC ĐĂNG NHẬP
    public class ReviewController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewController(AppDbContext context)
        {
            _context = context;
        }

        // ================= LẤY USER ID TỪ CLAIM =================
        private int? GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out int id) ? id : null;
        }

        // ================== TẠO ĐÁNH GIÁ (GET) ==================
        [HttpGet]
        public async Task<IActionResult> Create(int orderId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == orderId &&
                    o.UserId == userId.Value &&
                    o.Status == "Hoàn thành");

            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không hợp lệ hoặc chưa hoàn thành.";
                return RedirectToAction("History", "Order");
            }

            // Danh sách sản phẩm trong đơn
            var orderProducts = order.OrderDetails
                .Select(od => new ReviewProductViewModel
                {
                    OrderDetailId = od.OrderDetailId,
                    ProductId = od.ProductId,
                    ProductName = od.Product.ProductName,
                    MainImage = od.Product.MainImage,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice
                })
                .ToList();

            // Danh sách ProductId đã đánh giá
            var reviewedProductIds = await _context.Reviews
                .Where(r => r.UserId == userId.Value)
                .Select(r => r.ProductId)
                .ToListAsync();

            // Lọc sản phẩm CHƯA đánh giá
            var unreviewedProducts = orderProducts
                .Where(p => !reviewedProductIds.Contains(p.ProductId))
                .ToList();

            ViewBag.Order = order;
            ViewBag.OrderId = orderId;

            return View(unreviewedProducts);
        }

        // ================== LƯU ĐÁNH GIÁ (POST) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int orderId,
            int productId,
            int rating,
            string? comment)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            // Kiểm tra đơn hàng hợp lệ
            var orderExists = await _context.Orders.AnyAsync(o =>
                o.OrderId == orderId &&
                o.UserId == userId.Value &&
                o.Status == "Hoàn thành");

            if (!orderExists)
            {
                TempData["Error"] = "Đơn hàng không hợp lệ hoặc chưa hoàn thành.";
                return RedirectToAction("History", "Order");
            }

            // Kiểm tra đã đánh giá chưa
            var alreadyReviewed = await _context.Reviews.AnyAsync(r =>
                r.UserId == userId.Value &&
                r.ProductId == productId);

            if (alreadyReviewed)
            {
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này.";
                return RedirectToAction("Create", new { orderId });
            }

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Điểm đánh giá phải từ 1 đến 5.";
                return RedirectToAction("Create", new { orderId });
            }

            var review = new Review
            {
                UserId = userId.Value,
                ProductId = productId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đánh giá sản phẩm thành công!";
            return RedirectToAction("Create", new { orderId });
        }
    }
}
