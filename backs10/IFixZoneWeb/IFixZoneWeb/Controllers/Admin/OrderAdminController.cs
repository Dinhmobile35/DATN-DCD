using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers.Admin
{
    [Authorize(Roles = "Admin,Staff")] // 🔐 ADMIN + STAFF được xử lý đơn
    public class OrderAdminController : Controller
    {
        private readonly AppDbContext _context;

        public OrderAdminController(AppDbContext context)
        {
            _context = context;
        }

        // ================== DANH SÁCH ĐƠN HÀNG ==================
        // Tìm kiếm + lọc trạng thái + phân trang
        public async Task<IActionResult> Index(string? q, string? status, int page = 1)
        {
            const int pageSize = 15;
            if (page < 1) page = 1;

            var query = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            // 🔍 TÌM KIẾM
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(o =>
                    (o.OrderCode != null && o.OrderCode.ToLower().Contains(q)) ||
                    (o.RecipientName != null && o.RecipientName.ToLower().Contains(q)) ||
                    (o.RecipientPhone != null && o.RecipientPhone.Contains(q))
                );
            }

            // 🔍 LỌC TRẠNG THÁI
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                query = query.Where(o => o.Status == status);
            }

            var totalItems = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Query = q;
            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.OrderStatuses = new[]
            {
                "Mới",
                "Xác nhận",
                "Đang chuẩn bị",
                "Đang giao",
                "Hoàn thành",
                "Hủy"
            };

            return View(orders);
        }

        // ================== CHI TIẾT ĐƠN HÀNG ==================
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderStatusHistories)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // ================== CẬP NHẬT TRẠNG THÁI ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            if (string.IsNullOrWhiteSpace(newStatus))
            {
                return Json(new
                {
                    success = false,
                    message = "Trạng thái không hợp lệ"
                });
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy đơn hàng"
                });
            }

            // ❗ KHÔNG CẬP NHẬT NẾU TRÙNG TRẠNG THÁI
            if (order.Status == newStatus)
            {
                return Json(new
                {
                    success = false,
                    message = "Đơn hàng đã ở trạng thái này"
                });
            }

            // ===== CẬP NHẬT TRẠNG THÁI =====
            order.Status = newStatus;

            // ===== LƯU LỊCH SỬ =====
            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = id,
                Status = newStatus,
                UpdatedAt = DateTime.Now
            });

            // ===== 🔔 TẠO THÔNG BÁO CHO USER =====
            string notificationContent = newStatus switch
            {
                "Xác nhận" =>
                    $"Đơn hàng {order.OrderCode} đã được xác nhận.",

                "Đang chuẩn bị" =>
                    $"Đơn hàng {order.OrderCode} đang được chuẩn bị.",

                "Đang giao" =>
                    $"Đơn hàng {order.OrderCode} đang được giao.",

                "Hoàn thành" =>
                    $"Đơn hàng {order.OrderCode} đã hoàn thành. Cảm ơn bạn đã mua hàng!",

                "Hủy" =>
                    $"Đơn hàng {order.OrderCode} đã bị hủy.",

                _ =>
                    $"Trạng thái đơn hàng {order.OrderCode} đã được cập nhật."
            };

            _context.Notifications.Add(new Notification
            {
                UserId = order.UserId,
                Content = notificationContent,
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                newStatus
            });
        }

        // ================== IN ĐƠN HÀNG ==================
        // Trang riêng chỉ dùng để in
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
