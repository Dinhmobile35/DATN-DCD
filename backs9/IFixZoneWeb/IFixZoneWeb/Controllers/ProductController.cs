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
        // DANH SÁCH SẢN PHẨM (TẤT CẢ - TÌM KIẾM + LỌC DANH MỤC)
        // ===============================
        public IActionResult Index(string searchString, int? categoryId)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategoryId"] = categoryId;

            IQueryable<Product> query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)          // ⭐ BẮT BUỘC – FIX RATING
                .AsNoTracking();

            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var keyword = searchString.Trim().ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(keyword) ||
                    (p.Description != null && p.Description.ToLower().Contains(keyword))
                );
            }

            var minDate = new DateTime(1753, 1, 1);

            var products = query
                .OrderBy(p => p.Stock <= 0)                // Hết hàng xuống cuối
                .ThenByDescending(p => p.CreatedAt ?? minDate)
                .Take(40)
                .ToList();

            return View(products);
        }

        // ===============================
        // DANH SÁCH THEO DANH MỤC (CÓ BREADCRUMB)
        // ===============================
        public IActionResult Category(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
                return NotFound();

            var products = _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            ViewBag.Category = category;
            return View("Index", products);
        }

        // ===============================
        // CHI TIẾT SẢN PHẨM
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

            // SẢN PHẨM LIÊN QUAN (lấy 8 sản phẩm cùng danh mục, trừ chính nó)
            ViewBag.RelatedProducts = _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToList();

            return View(product);
        }

        // ===============================
        // API LẤY SẢN PHẨM LIÊN QUAN CHO SLIDER (AJAX)
        // ===============================
        [HttpGet]
        public IActionResult GetRelated(int productId)
        {
            var product = _context.Products.Find(productId);
            if (product == null)
                return Json(new List<object>());

            var related = _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != productId)
                .Take(8)
                .Select(p => new
                {
                    productId = p.ProductId,
                    productName = p.ProductName,
                    mainImage = p.MainImage,
                    price = p.Price,
                    rating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0  // Thêm rating trung bình
                })
                .ToList();

            return Json(related);
        }
    }
}