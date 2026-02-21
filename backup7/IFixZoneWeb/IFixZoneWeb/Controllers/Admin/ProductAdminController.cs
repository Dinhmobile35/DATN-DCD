using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers.Admin
{
    [Authorize(Roles = "Admin")] // 🔐 CHỈ ADMIN ĐƯỢC QUẢN LÝ SẢN PHẨM
    public class ProductAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductAdminController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ================== DANH SÁCH + TÌM KIẾM + PHÂN TRANG ==================
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => p.ProductName.Contains(q));
            }

            var total = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Query = q;

            return View(products);
        }

        // ================== FORM THÊM / SỬA SẢN PHẨM ==================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();

            if (id == null || id == 0)
                return View(new Product());

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model, IFormFile? imageFile)
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // ❗ BỎ VALIDATE NAVIGATION PROPERTY
            ModelState.Remove("Category");

            if (!ModelState.IsValid)
                return View(model);

            // ================== XỬ LÝ ẢNH ==================
            var uploadFolder = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            if (imageFile != null && imageFile.Length > 0)
            {
                var ext = Path.GetExtension(imageFile.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // ❗ XÓA ẢNH CŨ (NẾU CÓ)
                if (!string.IsNullOrEmpty(model.MainImage))
                {
                    var oldPath = Path.Combine(uploadFolder, model.MainImage);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                model.MainImage = fileName;
            }

            // ================== SAVE DB ==================
            if (model.ProductId == 0)
            {
                model.CreatedAt = DateTime.Now;
                _context.Products.Add(model);
            }
            else
            {
                _context.Products.Update(model);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Lưu sản phẩm thành công";

            return RedirectToAction(nameof(Index));
        }

        // ================== XÓA SẢN PHẨM ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            if (!string.IsNullOrEmpty(product.MainImage))
            {
                var path = Path.Combine(_env.WebRootPath, "images", "products", product.MainImage);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa sản phẩm";
            return RedirectToAction(nameof(Index));
        }

        // ================== QUẢN LÝ THÔNG SỐ KỸ THUẬT ==================
        [HttpGet]
        public async Task<IActionResult> ManageSpecifications(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductSpecifications)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSpecification(int productId, string specName, string specValue)
        {
            if (string.IsNullOrWhiteSpace(specName) || string.IsNullOrWhiteSpace(specValue))
            {
                TempData["Error"] = "Tên thông số và giá trị không được để trống";
                return RedirectToAction(nameof(ManageSpecifications), new { productId });
            }

            var exists = await _context.Products.AnyAsync(p => p.ProductId == productId);
            if (!exists) return NotFound();

            _context.ProductSpecifications.Add(new ProductSpecification
            {
                ProductId = productId,
                SpecName = specName.Trim(),
                SpecValue = specValue.Trim()
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm thông số kỹ thuật";

            return RedirectToAction(nameof(ManageSpecifications), new { productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpecification(int specId, string specName, string specValue)
        {
            var spec = await _context.ProductSpecifications.FindAsync(specId);
            if (spec == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(specName) || string.IsNullOrWhiteSpace(specValue))
            {
                TempData["Error"] = "Tên thông số và giá trị không được để trống";
                return RedirectToAction(nameof(ManageSpecifications), new { productId = spec.ProductId });
            }

            spec.SpecName = specName.Trim();
            spec.SpecValue = specValue.Trim();

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật thông số kỹ thuật thành công";

            return RedirectToAction(nameof(ManageSpecifications), new { productId = spec.ProductId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpecification(int specId)
        {
            var spec = await _context.ProductSpecifications.FindAsync(specId);
            if (spec == null)
                return NotFound();

            var productId = spec.ProductId;
            _context.ProductSpecifications.Remove(spec);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa thông số kỹ thuật";
            return RedirectToAction(nameof(ManageSpecifications), new { productId });
        }
    }
}
