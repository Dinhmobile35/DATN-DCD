using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace IFixZoneWeb.Controllers
{
    [Authorize(Roles = "Staff")]
    public class StaffController : Controller
    {
        private readonly AppDbContext _context;

        public StaffController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            // ===== THỐNG KÊ ĐƠN =====
            var totalOrders = _context.Orders.Count();
            var newOrders = _context.Orders.Count(o => o.Status == "Mới");
            var preparingOrders = _context.Orders.Count(o => o.Status == "Đang chuẩn bị");
            var shippingOrders = _context.Orders.Count(o => o.Status == "Đang giao");
            var completedOrders = _context.Orders.Count(o => o.Status == "Hoàn thành");

            // ===== DOANH THU (THAM KHẢO CHO STAFF) =====
            var revenue = _context.Orders
                .Where(o => o.Status == "Hoàn thành")
                .Sum(o => (decimal?)o.TotalAmount) ?? 0;

            // ===== ĐƠN GẦN NHẤT =====
            var latestOrders = _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .ToList();

            var model = new
            {
                totalOrders,
                newOrders,
                preparingOrders,
                shippingOrders,
                completedOrders,
                revenue,
                latestOrders
            };

            return View(model);
            return View(model);
        }

        // ================= QUẢN LÝ ĐƠN HÀNG =================
        public async Task<IActionResult> Orders(string status = "All")
        {
            var query = _context.Orders
                .Include(o => o.User)
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate);

            if (status != "All" && !string.IsNullOrEmpty(status))
            {
                // Nếu status có dấu tiếng Việt, cần đảm bảo query string đúng
                // Ở đây demo đơn giản, nếu status = "Mới" thì lọc Mới
                query = (IOrderedQueryable<Order>)query.Where(o => o.Status == status);
            }

            var orders = await query.ToListAsync();
            ViewBag.CurrentStatus = status;

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("Orders");
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Cập nhật đơn #{order.OrderCode} thành công!";
            return RedirectToAction("Orders", new { status = ViewBag.CurrentStatus ?? "All" });
        }

        // ================= CHI TIẾT ĐƠN HÀNG =================
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Include(o => o.OrderStatusHistories)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("Orders");
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("Orders");
            }

            order.Status = newStatus;
            
            // Lưu lịch sử nếu cần (Model có OrderStatusHistories)
            // _context.OrderStatusHistories.Add(...)

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái thành công";
            return RedirectToAction("Details", new { id });
        }

        // ================== IN ĐƠN HÀNG ==================
        public async Task<IActionResult> Print(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}
