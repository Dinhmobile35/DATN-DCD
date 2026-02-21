using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // ===============================
        // ⭐ DANH SÁCH SẢN PHẨM
        // HƯỚNG 2 – CLICK CHA / CON / CHÁU → RA HẾT
        // ===============================
        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategoryId"] = categoryId;

            // =========================
            // 📂 LOAD TOÀN BỘ CATEGORY
            // =========================
            var allCategories = await _context.Categories
                .AsNoTracking()
                .ToListAsync();

            // =========================
            // 📌 BUILD DANH SÁCH CATEGORY ID (CHA + CON + CHÁU)
            // =========================
            List<int> categoryIds = new();

            if (categoryId.HasValue && categoryId > 0)
            {
                void CollectChildren(int id)
                {
                    categoryIds.Add(id);

                    var children = allCategories
                        .Where(c => c.ParentId == id)
                        .Select(c => c.CategoryId)
                        .ToList();

                    foreach (var childId in children)
                        CollectChildren(childId);
                }

                CollectChildren(categoryId.Value);
            }

            // =========================
            // 📦 QUERY SẢN PHẨM
            // =========================
            IQueryable<Product> query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews) // ⭐ rating
                .AsNoTracking()
                .Where(p => p.Status == "Active");

            // 🔹 LỌC THEO CATEGORY TREE
            if (categoryIds.Any())
            {
                query = query.Where(p => categoryIds.Contains(p.CategoryId));
            }

            // 🔹 SEARCH
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var keyword = searchString.Trim().ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(keyword) ||
                    (p.Description != null && p.Description.ToLower().Contains(keyword))
                );
            }

            var minDate = new DateTime(1753, 1, 1);

            var products = await query
                .OrderBy(p => p.Stock <= 0)                // Hết hàng xuống cuối
                .ThenByDescending(p => p.CreatedAt ?? minDate)
                .Take(40)
                .ToListAsync();

            return View(products);
        }

        // ===============================
        // ⚠️ KHÔNG CẦN DÙNG NỮA (HƯỚNG 2)
        // Có thể xóa nếu muốn
        // ===============================
        public IActionResult Category(int id)
        {
            return RedirectToAction("Index", new { categoryId = id });
        }

        // ===============================
        // 📦 CHI TIẾT SẢN PHẨM
        // ===============================
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSpecifications)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            // =========================
            // 🧭 BREADCRUMB (CHA → CON → CHÁU)
            // =========================
            if (product.Category != null)
            {
                ViewBag.Breadcrumb = BuildCategoryBreadcrumb(product.Category);
            }
            else
            {
                ViewBag.Breadcrumb = new List<Category>();
            }

            // (OPTIONAL) TÊN SẢN PHẨM
            ViewBag.CurrentProductName = product.ProductName;

            // =========================
            // 🔹 SẢN PHẨM LIÊN QUAN
            // =========================
            ViewBag.RelatedProducts = _context.Products
                .Include(p => p.Reviews)
                .Where(p => p.CategoryId == product.CategoryId &&
                            p.ProductId != product.ProductId &&
                            p.Status == "Active")
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToList();

            return View(product);
        }

        // ===============================
        // 🔁 API – SẢN PHẨM LIÊN QUAN (AJAX)
        // ===============================
        [HttpGet]
        public IActionResult GetRelated(int productId)
        {
            var product = _context.Products.Find(productId);
            if (product == null)
                return Json(new List<object>());

            var related = _context.Products
                .Include(p => p.Reviews)
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != productId)
                .Take(8)
                .Select(p => new
                {
                    productId = p.ProductId,
                    productName = p.ProductName,
                    mainImage = p.MainImage,
                    price = p.Price,
                    rating = p.Reviews.Any(r => r.Rating.HasValue)
                        ? p.Reviews.Average(r => r.Rating)
                        : 0
                })
                .ToList();

            return Json(related);
        }
        // ===============================
        // 🧭 BUILD BREADCRUMB CATEGORY
        // ===============================
        private List<Category> BuildCategoryBreadcrumb(Category category)
        {
            var result = new List<Category>();

            while (category != null)
            {
                result.Insert(0, category);

                if (!category.ParentId.HasValue)
                    break;

                category = _context.Categories
                    .AsNoTracking()
                    .FirstOrDefault(c => c.CategoryId == category.ParentId);
            }

            return result;
        }

    }
}
