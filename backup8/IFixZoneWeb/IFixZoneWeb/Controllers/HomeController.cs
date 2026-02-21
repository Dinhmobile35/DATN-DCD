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

        // =====================================================
        // ⭐ TRANG CHỦ
        // =====================================================
        public async Task<IActionResult> Index(int? categoryId)
        {
            // =================================================
            // 📂 LOAD DANH MỤC
            // =================================================
            var categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.CurrentCategory = categoryId;

            // =================================================
            // 📦 BASE QUERY – SẢN PHẨM ACTIVE
            // =================================================
            var baseQuery = _context.Products
                .Include(p => p.Category)
                .Where(p => p.Status == "Active")
                .AsQueryable();

            // =================================================
            // 🆕 SẢN PHẨM MỚI (TỐI ĐA 10)
            // =================================================
            ViewBag.NewProducts = await baseQuery
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToListAsync();

            // =================================================
            // 🔥 SẢN PHẨM ĐỀ XUẤT (BÁN CHẠY – TỐI ĐA 10)
            // =================================================
            ViewBag.Recommended = await _context.Products
                .Include(p => p.Category)
                .Where(p =>
                    p.Status == "Active" &&
                    (p.Stock == null || p.Stock > 0)
                )
                .OrderByDescending(p => p.Stock)
                .Take(10)
                .ToListAsync();

            // =================================================
            // ⭐ SẢN PHẨM NỔI BẬT (SLIDER – TỐI ĐA 10)
            // =================================================
            ViewBag.FeaturedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Status == "Active")
                .OrderByDescending(p => p.Stock)
                .Take(10)
                .ToListAsync();

            // =================================================
            // ✅ TẤT CẢ SẢN PHẨM ACTIVE (DÙNG CHIA SECTION)
            // ⚠ KHÔNG lọc category ở đây
            // ⚠ View sẽ Take(10) theo từng section
            // =================================================
            ViewBag.AllProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Status == "Active")
                .ToListAsync();

            return View();
        }

        // =====================================================
        // 📞 TRANG LIÊN HỆ
        // =====================================================
        public IActionResult Contact()
        {
            return View();
        }
    }
}
