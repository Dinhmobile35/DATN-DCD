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
            // 1️⃣ Validate
            if (string.IsNullOrWhiteSpace(newStatus))
            {
                TempData["Error"] = "Trạng thái không hợp lệ";
                return RedirectToAction("Details", new { id });
            }

            // 2️⃣ Lấy đơn hàng
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderStatusHistories)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("Index");
            }

            // 3️⃣ Không cập nhật nếu trùng trạng thái
            if (order.Status == newStatus)
            {
                TempData["Error"] = "Đơn hàng đã ở trạng thái này";
                return RedirectToAction("Details", new { id });
            }

            // 4️⃣ Cập nhật trạng thái hiện tại
            order.Status = newStatus;

            // 5️⃣ Lưu lịch sử trạng thái
            order.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                Status = newStatus,
                UpdatedAt = DateTime.Now
            });

            // 6️⃣ Nội dung thông báo cho user
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

            // 7️⃣ Lưu thông báo
            _context.Notifications.Add(new Notification
            {
                UserId = order.UserId,
                Content = notificationContent,
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // 8️⃣ Thông báo + quay lại chi tiết
            TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công";
            return RedirectToAction("Details", new { id = order.OrderId });
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
