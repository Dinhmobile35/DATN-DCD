using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers.Admin
{
    public class OrderAdminController : Controller
    {
        private readonly AppDbContext _context;

        public OrderAdminController(AppDbContext context)
        {
            _context = context;
        }

        // DANH SÁCH ĐƠN HÀNG + TÌM KIẾM + LỌC + PHÂN TRANG
        public async Task<IActionResult> Index(string? q, string? status, int page = 1)
        {
            int pageSize = 15;

            var query = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                query = query.Where(o =>
                    (o.OrderCode != null && o.OrderCode.ToLower().Contains(q)) ||
                    (o.RecipientName != null && o.RecipientName.ToLower().Contains(q)) ||
                    (o.RecipientPhone != null && o.RecipientPhone.Contains(q))
                );
            }

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
                "Mới", "Xác nhận", "Đang chuẩn bị", "Đang giao", "Hoàn thành", "Hủy"
            };

            return View(orders);
        }

        // CHI TIẾT ĐƠN HÀNG
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

        // CẬP NHẬT TRẠNG THÁI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            if (string.IsNullOrWhiteSpace(newStatus))
                return Json(new { success = false, message = "Trạng thái không hợp lệ" });

            order.Status = newStatus;

            var history = new OrderStatusHistory
            {
                OrderId = id,
                Status = newStatus,
                UpdatedAt = DateTime.Now
            };

            _context.OrderStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            return Json(new { success = true, newStatus });
        }
        // IN ĐƠN HÀNG RIÊNG (TRANG CHỈ DÙNG ĐỂ IN)
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