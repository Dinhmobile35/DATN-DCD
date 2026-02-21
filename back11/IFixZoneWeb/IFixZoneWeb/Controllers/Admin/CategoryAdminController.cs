using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers.Admin
{
    // ✅ ADMIN + STAFF xem / sửa – ❌ chỉ ADMIN được xóa
    [Authorize(Roles = "Admin,Staff")]
    public class CategoryAdminController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryAdminController(AppDbContext context)
        {
            _context = context;
        }

        /* =========================================================
           INDEX – SEARCH + FILTER ROOT + TREE VIEW
        ========================================================= */
        public async Task<IActionResult> Index(string? q, int? rootId)
        {
            var query = _context.Categories
                .Include(c => c.Products)
                .AsQueryable();

            // 🔍 SEARCH
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(c => c.CategoryName.Contains(q));
            }

            // 🌱 FILTER THEO DANH MỤC GỐC (CHA + CON + CHÁU)
            if (rootId.HasValue)
            {
                var ids = GetAllChildCategoryIds(rootId.Value);
                ids.Add(rootId.Value);
                query = query.Where(c => ids.Contains(c.CategoryId));
            }

            var data = await query
                .OrderBy(c => c.ParentId == null ? 0 : 1)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            // 🔢 MAP TỔNG SẢN PHẨM (CHA + CON + CHÁU)
            var totalMap = new Dictionary<int, int>();
            foreach (var c in data)
            {
                totalMap[c.CategoryId] = GetTotalProductCount(c.CategoryId);
            }

            ViewBag.TotalMap = totalMap;

            // 🌱 DROPDOWN DANH MỤC GỐC
            ViewBag.RootCategories = await _context.Categories
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Query = q;
            ViewBag.RootId = rootId;

            return View(data);
        }

        /* =========================================================
           AJAX – LOAD CHILDREN (KHÔNG LINQ C# TRONG SQL)
        ========================================================= */
        [HttpGet]
        public async Task<IActionResult> LoadChildren(int parentId)
        {
            // ⚠️ CHỈ QUERY SQL
            var children = await _context.Categories
                .Where(c => c.ParentId == parentId)
                .ToListAsync();

            // ✅ XỬ LÝ LOGIC SAU KHI LOAD
            var result = children.Select(c => new
            {
                categoryId = c.CategoryId,
                categoryName = c.CategoryName,
                productCount = GetTotalProductCount(c.CategoryId),
                hasChildren = _context.Categories.Any(x => x.ParentId == c.CategoryId)
            }).ToList();

            return Json(result);
        }

        /* =========================================================
           TOTAL PRODUCT COUNT (CHA + CON + CHÁU)
        ========================================================= */
        private int GetTotalProductCount(int categoryId)
        {
            var ids = GetAllChildCategoryIds(categoryId);
            ids.Add(categoryId);

            return _context.Products.Count(p => ids.Contains(p.CategoryId));
        }

        /* =========================================================
           ĐỆ QUY LẤY TẤT CẢ CATEGORY CON
        ========================================================= */
        private List<int> GetAllChildCategoryIds(int parentId)
        {
            var result = new List<int>();

            var children = _context.Categories
                .Where(c => c.ParentId == parentId)
                .Select(c => c.CategoryId)
                .ToList();

            foreach (var childId in children)
            {
                result.Add(childId);
                result.AddRange(GetAllChildCategoryIds(childId));
            }

            return result;
        }

        /* =========================================================
           EDIT – GET
        ========================================================= */
        public async Task<IActionResult> Edit(int? id)
        {
            // ⚠️ CHO PHÉP CHỌN MỌI CATEGORY LÀM CHA (TRỪ CHÍNH NÓ)
            var parentCategories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            if (id == null || id == 0)
            {
                ViewBag.ParentCategories = parentCategories;
                return View(new Category());
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            ViewBag.ParentCategories = parentCategories
                .Where(c => c.CategoryId != category.CategoryId)
                .ToList();

            return View(category);
        }

        /* =========================================================
           EDIT – POST
        ========================================================= */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category model)
        {
            var parentCategories = await _context.Categories
                .Where(c => c.CategoryId != model.CategoryId)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                ViewBag.ParentCategories = parentCategories;
                return View(model);
            }

            // ❗ TRÙNG TÊN
            var exists = await _context.Categories.AnyAsync(x =>
                x.CategoryName == model.CategoryName &&
                x.CategoryId != model.CategoryId);

            if (exists)
            {
                ModelState.AddModelError("CategoryName", "Tên danh mục đã tồn tại");
                ViewBag.ParentCategories = parentCategories;
                return View(model);
            }

            if (model.CategoryId == 0)
                _context.Categories.Add(model);
            else
                _context.Categories.Update(model);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Lưu danh mục thành công";

            return RedirectToAction(nameof(Index));
        }

        /* =========================================================
           DELETE – CHỈ ADMIN
        ========================================================= */
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound();

            if (await _context.Categories.AnyAsync(c => c.ParentId == id))
            {
                TempData["Error"] = "Không thể xóa danh mục đang có danh mục con!";
                return RedirectToAction(nameof(Index));
            }

            if (category.Products.Any())
            {
                TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm!";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa danh mục thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}
