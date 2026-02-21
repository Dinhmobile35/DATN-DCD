using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers.Admin
{
    public class ProductAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductAdminController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // DANH SÁCH + TÌM KIẾM + PHÂN TRANG
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10)
        {
            var query = _context.Products
                .Include(x => x.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.ProductName.Contains(q));

            var total = await query.CountAsync();
            var products = await query
                .OrderByDescending(x => x.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Query = q;

            return View(products);
        }

        // FORM THÊM/SỬA SẢN PHẨM
        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();

            Product model;
            if (id == null || id == 0)
            {
                model = new Product();
            }
            else
            {
                model = await _context.Products.FindAsync(id);
                if (model == null) return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model, IFormFile? imageFile)
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();

            ModelState.Remove("Category");

            if (!ModelState.IsValid)
                return View(model);

            var folder = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(folder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await imageFile.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(model.MainImage))
                {
                    var oldPath = Path.Combine(folder, model.MainImage);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                model.MainImage = fileName;
            }

            if (model.ProductId == 0)
                _context.Products.Add(model);
            else
                _context.Update(model);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // XÓA SẢN PHẨM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (!string.IsNullOrEmpty(product.MainImage))
            {
                var path = Path.Combine(_env.WebRootPath, "images", "products", product.MainImage);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // QUẢN LÝ THÔNG SỐ KỸ THUẬT
        [HttpGet]
        public async Task<IActionResult> ManageSpecifications(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductSpecifications)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSpecification(int productId, string specName, string specValue)
        {
            if (string.IsNullOrWhiteSpace(specName) || string.IsNullOrWhiteSpace(specValue))
            {
                TempData["Error"] = "Tên thông số và giá trị không được để trống.";
                return RedirectToAction(nameof(ManageSpecifications), new { productId });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var spec = new ProductSpecification
            {
                ProductId = productId,
                SpecName = specName.Trim(),
                SpecValue = specValue.Trim()
            };

            _context.ProductSpecifications.Add(spec);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm thông số kỹ thuật mới.";
            return RedirectToAction(nameof(ManageSpecifications), new { productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpecification(int specId, string specName, string specValue)
        {
            var spec = await _context.ProductSpecifications.FindAsync(specId);
            if (spec == null) return NotFound();

            if (string.IsNullOrWhiteSpace(specName) || string.IsNullOrWhiteSpace(specValue))
            {
                TempData["Error"] = "Tên thông số và giá trị không được để trống.";
                return RedirectToAction(nameof(ManageSpecifications), new { productId = spec.ProductId });
            }

            spec.SpecName = specName.Trim();
            spec.SpecValue = specValue.Trim();

            _context.Update(spec);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật thông số kỹ thuật.";
            return RedirectToAction(nameof(ManageSpecifications), new { productId = spec.ProductId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSpecification(int specId)
        {
            var spec = await _context.ProductSpecifications.FindAsync(specId);
            if (spec == null) return NotFound();

            var productId = spec.ProductId;
            _context.ProductSpecifications.Remove(spec);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa thông số kỹ thuật.";
            return RedirectToAction(nameof(ManageSpecifications), new { productId });
        }
    }
}