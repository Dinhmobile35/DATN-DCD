using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers.Admin
{
    [Authorize(Roles = "Admin")] // 🔐 CHỈ ADMIN
    public class CategoryAdminController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryAdminController(AppDbContext context)
        {
            _context = context;
        }

        // ================== DANH SÁCH + TÌM KIẾM + PHÂN TRANG ==================
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => x.CategoryName.Contains(q));
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.CategoryId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Query = q;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(data);
        }

        // ================== THÊM / SỬA (GET) ==================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0)
                return View(new Category());

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        // ================== THÊM / SỬA (POST) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ❗ TRÁNH TRÙNG TÊN CATEGORY
            var exists = await _context.Categories.AnyAsync(x =>
                x.CategoryName == model.CategoryName &&
                x.CategoryId != model.CategoryId);

            if (exists)
            {
                ModelState.AddModelError("CategoryName", "Tên danh mục đã tồn tại");
                return View(model);
            }

            if (model.CategoryId == 0)
            {
                _context.Categories.Add(model);
            }
            else
            {
                _context.Categories.Update(model);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Lưu danh mục thành công!";

            return RedirectToAction(nameof(Index));
        }

        // ================== XÓA ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(x => x.CategoryId == id);

            if (category == null)
                return NotFound();

            // ❗ KHÔNG CHO XÓA NẾU CÓ SẢN PHẨM
            if (category.Products != null && category.Products.Any())
            {
                TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm!";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa danh mục thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
