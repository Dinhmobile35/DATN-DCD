using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers
{
    public class ReviewController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Tạo đánh giá cho đơn hàng (danh sách sản phẩm chưa đánh giá)
        [HttpGet]
        public async Task<IActionResult> Create(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId.Value);

            if (order == null || order.Status != "Hoàn thành")
            {
                TempData["Error"] = "Đơn hàng không hợp lệ hoặc chưa hoàn thành.";
                return RedirectToAction("History", "Order");
            }

            // Lấy danh sách sản phẩm trong đơn hàng
            var orderProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderId == orderId)
                .Select(od => new ReviewProductViewModel
                {
                    OrderDetailId = od.OrderDetailId,
                    ProductId = od.ProductId,
                    ProductName = od.Product.ProductName,
                    MainImage = od.Product.MainImage,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice
                })
                .ToListAsync();

            // Lấy danh sách ProductId đã đánh giá của user (trong bất kỳ đơn hàng nào)
            var reviewedProductIds = await _context.Reviews
                .Where(r => r.UserId == userId.Value)
                .Select(r => r.ProductId)
                .ToListAsync();

            // Lọc sản phẩm chưa đánh giá
            var unreviewed = orderProducts
                .Where(p => !reviewedProductIds.Contains(p.ProductId))
                .ToList();

            ViewBag.Order = order;
            ViewBag.OrderId = orderId;
            return View(unreviewed);
        }

        // POST: Lưu đánh giá cho một sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int orderId, int productId, int rating, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Unauthorized();

            // Kiểm tra đơn hàng hợp lệ & thuộc user & hoàn thành
            var orderExists = await _context.Orders
                .AnyAsync(o => o.OrderId == orderId && o.UserId == userId.Value && o.Status == "Hoàn thành");

            if (!orderExists)
            {
                TempData["Error"] = "Đơn hàng không hợp lệ hoặc chưa hoàn thành.";
                return RedirectToAction("History", "Order");
            }

            // Kiểm tra đã đánh giá sản phẩm này chưa (dựa trên User + Product)
            var alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.UserId == userId.Value && r.ProductId == productId);

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