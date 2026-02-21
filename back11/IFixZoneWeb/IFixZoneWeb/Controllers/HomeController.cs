using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // ⭐ TRANG CHỦ – HƯỚNG 2
        // =========================
        public async Task<IActionResult> Index()
        {
            // =========================
            // 📂 DANH MỤC (DÙNG CHO MENU + VIEW)
            // =========================
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = categories;

            // =========================
            // 📦 BASE QUERY – TOÀN BỘ SẢN PHẨM ACTIVE
            // ❌ KHÔNG LỌC CATEGORY Ở ĐÂY
            // =========================
            var productQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .AsNoTracking()
                .Where(p => p.Status == "Active");

            // =========================
            // 🆕 SẢN PHẨM MỚI
            // =========================
            ViewBag.NewProducts = await productQuery
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToListAsync();

            // =========================
            // ⭐ SẢN PHẨM ĐỀ XUẤT (>= 4 SAO)
            // =========================
            ViewBag.RecommendedProducts = await productQuery
                .Where(p => p.Reviews.Any(r => r.Rating.HasValue))
                .Select(p => new
                {
                    Product = p,
                    AvgRating = p.Reviews
                        .Where(r => r.Rating.HasValue)
                        .Average(r => r.Rating!.Value)
                })
                .Where(x => x.AvgRating >= 4)
                .OrderByDescending(x => x.AvgRating)
                .ThenByDescending(x => x.Product.CreatedAt)
                .Take(10)
                .Select(x => x.Product)
                .ToListAsync();

            // =========================
            // 🔥 SẢN PHẨM NỔI BẬT
            // =========================
            ViewBag.FeaturedProducts = await productQuery
                .Where(p =>
                    (p.Stock == null || p.Stock > 0) &&
                    p.Reviews.Any(r => r.Rating.HasValue)
                )
                .OrderByDescending(p =>
                    p.Reviews
                        .Where(r => r.Rating.HasValue)
                        .Average(r => r.Rating!.Value)
                )
                .ThenByDescending(p =>
                    p.Reviews.Count(r => r.Rating.HasValue)
                )
                .ThenByDescending(p => p.CreatedAt)
                .Take(10)
                .ToListAsync();

            // =========================
            // 📦 TẤT CẢ SẢN PHẨM
            // 👉 VIEW SẼ TỰ CHIA THEO DANH MỤC CHA
            // =========================
            ViewBag.AllProducts = await productQuery.ToListAsync();

            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}
