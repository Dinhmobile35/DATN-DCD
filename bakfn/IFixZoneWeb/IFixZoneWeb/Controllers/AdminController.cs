using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace IFixZoneWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ================= DASHBOARD =================
        public IActionResult Dashboard()
        {
            // ===== KPI =====
            var totalUsers = _context.Users.Count();
            var totalProducts = _context.Products.Count();
            var totalOrders = _context.Orders.Count();

            var totalRevenue = _context.Orders
                .Where(o => o.Status == "Hoàn thành")
                .Sum(o => (decimal?)o.TotalAmount) ?? 0;

            // ===== DOANH THU 7 NGÀY GẦN NHẤT =====
            var revenue7Days = _context.Orders
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate != null)
                .GroupBy(o => o.OrderDate!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .Take(7)
                .ToList();

            // ===== THỐNG KÊ TRẠNG THÁI ĐƠN =====
            var orderStatusStats = _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // ===== TOP 5 SẢN PHẨM BÁN CHẠY =====
            var topProducts = _context.OrderDetails
                .Include(d => d.Product)
                .GroupBy(d => d.Product!.ProductName)
                .Select(g => new
                {
                    Product = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToList();

            // ===== VIEW MODEL ẨN DANH =====
            var model = new
            {
                totalUsers,
                totalProducts,
                totalOrders,
                totalRevenue,

                RevenueLabels = revenue7Days.Select(x => x.Date.ToString("dd/MM")).ToList(),
                RevenueData = revenue7Days.Select(x => x.Revenue).ToList(),

                StatusLabels = orderStatusStats.Select(x => x.Status).ToList(),
                StatusData = orderStatusStats.Select(x => x.Count).ToList(),

                TopProducts = topProducts
            };

            return View(model);
        }
    }
}
