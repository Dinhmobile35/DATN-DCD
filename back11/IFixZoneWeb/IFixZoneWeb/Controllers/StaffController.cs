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
        }
    }
}
