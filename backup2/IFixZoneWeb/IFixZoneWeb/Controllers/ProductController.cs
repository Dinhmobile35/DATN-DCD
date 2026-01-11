using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace IFixZoneWeb.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // DANH SÁCH SẢN PHẨM - HỖ TRỢ LỌC THEO DANH MỤC & TÌM KIẾM
        public IActionResult Index(string searchString, int? categoryId)
        {
            // Lưu từ khóa tìm kiếm và categoryId để dùng lại trong view (ví dụ: active menu, ô search)
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategoryId"] = categoryId;

            IQueryable<Product> productsQuery = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // LỌC THEO DANH MỤC (khi click vào danh mục nổi bật)
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // LỌC THEO TỪ KHÓA TÌM KIẾM
            if (!string.IsNullOrEmpty(searchString))
            {
                string search = searchString.Trim().ToLower();
                productsQuery = productsQuery.Where(p =>
                    p.ProductName.ToLower().Contains(search) ||
                    (p.Description != null && p.Description.ToLower().Contains(search))
                );
            }

            // Sắp xếp theo ngày tạo (mới nhất trước)
            var minAllowedDate = new DateTime(1753, 1, 1);
            var products = productsQuery
                .OrderByDescending(p => p.CreatedAt ?? minAllowedDate)
                .Take(40) // Tăng lên 40 để hiển thị nhiều hơn, có thể phân trang sau
                .ToList();

            return View(products);
        }

        // CHI TIẾT SẢN PHẨM
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSpecifications)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User) // Nếu muốn hiển thị tên người review
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}