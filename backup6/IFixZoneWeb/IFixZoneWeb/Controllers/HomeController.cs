using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFixZoneWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // ⭐ Trang chủ
        public async Task<IActionResult> Index(int? categoryId)
        {
            // 👉 Load danh mục
            ViewBag.Categories = await _context.Categories
                .OrderBy(x => x.CategoryName)
                .ToListAsync();

            // 👉 Query sản phẩm
            var query = _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            // 👉 Nếu chọn danh mục → lọc
            if (categoryId != null)
            {
                query = query.Where(p => p.CategoryId == categoryId);
                ViewBag.CurrentCategory = categoryId;
            }

            // ⭐ Sản phẩm mới
            ViewBag.NewProducts = await query.Take(8).ToListAsync();

            // ⭐ Sản phẩm đề xuất (bán chạy / xem nhiều)
            ViewBag.Recommended = await _context.Products
                .OrderByDescending(p => p.Stock)   // tạm coi stock nhiều → phổ biến
                .Take(4)
                .ToListAsync();

            return View();
        }
    }
}
