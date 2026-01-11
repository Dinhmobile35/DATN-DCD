using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // ========== LỊCH SỬ ĐƠN HÀNG ==========
        [HttpGet]
        public async Task<IActionResult> History(string status = "Tất cả")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var query = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderStatusHistories)
                .Where(o => o.UserId == userId.Value);

            // Lọc theo trạng thái (nếu không phải "Tất cả")
            if (status != "Tất cả")
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(orders);
        }

        // ========== CHI TIẾT ĐƠN HÀNG ==========
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderStatusHistories)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId.Value);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // ========== HỦY ĐƠN (nếu trạng thái "Mới" hoặc "Chờ thanh toán") ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId.Value);

            if (order == null || (order.Status != "Mới" && order.Status != "Chờ thanh toán"))
                return Json(new { success = false, message = "Không thể hủy đơn hàng này" });

            // Cộng lại tồn kho
            foreach (var detail in order.OrderDetails)
            {
                detail.Product.Stock += detail.Quantity;
            }

            order.Status = "Đã hủy";
            await _context.SaveChangesAsync();

            // Thêm lịch sử trạng thái
            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                Status = "Đã hủy",
                UpdatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đơn hàng đã được hủy thành công!";
            return RedirectToAction("History");
        }
    }
}