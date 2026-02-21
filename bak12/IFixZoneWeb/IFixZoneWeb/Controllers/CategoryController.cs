using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // 🔹 ENTRY DUY NHẤT – CLICK VÀO BẤT KỲ DANH MỤC NÀO
        // URL: /Category/Index/{id}
        // =====================================================
        public async Task<IActionResult> Index(int id)
        {
            // 1️⃣ DANH MỤC HIỆN TẠI
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound();

            // =================================================
            // 2️⃣ BUILD BREADCRUMB (TỪ GỐC → HIỆN TẠI)
            // =================================================
            var breadcrumb = new List<Category>();
            var current = category;

            while (current != null)
            {
                breadcrumb.Insert(0, current);

                if (current.ParentId == null)
                    break;

                current = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CategoryId == current.ParentId);
            }

            ViewBag.Breadcrumb = breadcrumb;
            ViewBag.CurrentCategory = category;

            // =================================================
            // 3️⃣ KIỂM TRA DANH MỤC CON
            // =================================================
            var children = await _context.Categories
                .AsNoTracking()
                .Where(c => c.ParentId == id)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // =================================================
            // 👉 CÒN CON → TRANG CHỌN THIẾT BỊ (GIỐNG IFIXIT)
            // =================================================
            if (children.Any())
            {
                return View("Index", children); // Views/Category/Index.cshtml
            }

            // =================================================
            // 👉 LÁ CUỐI → HIỂN THỊ SẢN PHẨM
            // =================================================
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .AsNoTracking()
                .Where(p =>
                    p.Status == "Active" &&
                    p.CategoryId == id
                )
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Category = category;
            return View("Products", products); // Views/Category/Products.cshtml
        }
    }
}
