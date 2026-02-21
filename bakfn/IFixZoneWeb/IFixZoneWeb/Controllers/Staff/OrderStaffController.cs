using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers.Staff
{
    [Authorize(Roles = "Staff")]
    public class OrderStaffController : Controller
    {
        private readonly AppDbContext _context;

        public OrderStaffController(AppDbContext context)
        {
            _context = context;
        }

        /* ================== DANH SÁCH ĐƠN ================== */
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

            // ✅ STAFF ĐƯỢC HỦY
            ViewBag.OrderStatuses = new[]
            {
                "Mới",
                "Xác nhận",
                "Đang chuẩn bị",
                "Đang giao",
                "Hoàn thành",
                "Hủy"
            };

            return View(orders); // 👉 Views/OrderStaff/Index.cshtml
        }

        /* ================== CHI TIẾT ĐƠN ================== */
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

            return View(order); // 👉 Views/OrderStaff/Details.cshtml
        }

        /* ================== CẬP NHẬT TRẠNG THÁI ================== */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            if (string.IsNullOrWhiteSpace(newStatus))
            {
                TempData["Error"] = "Trạng thái không hợp lệ";
                return RedirectToAction("Details", new { id });
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderStatusHistories)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("Index");
            }

            if (order.Status == newStatus)
            {
                TempData["Error"] = "Đơn hàng đã ở trạng thái này";
                return RedirectToAction("Details", new { id });
            }

            // ✅ CẬP NHẬT
            order.Status = newStatus;

            order.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                Status = newStatus,
                UpdatedAt = DateTime.Now
            });

            // 🔔 THÔNG BÁO USER
            string content = newStatus switch
            {
                "Hủy" => $"Đơn hàng {order.OrderCode} đã bị hủy.",
                "Hoàn thành" => $"Đơn hàng {order.OrderCode} đã hoàn thành.",
                _ => $"Trạng thái đơn hàng {order.OrderCode} đã được cập nhật."
            };

            _context.Notifications.Add(new Notification
            {
                UserId = order.UserId,
                Content = content,
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái thành công";
            return RedirectToAction("Details", new { id });
        }

        /* ================== IN ĐƠN ================== */
        public async Task<IActionResult> Print(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return View(order); // 👉 Views/OrderStaff/Print.cshtml
        }
    }
}
