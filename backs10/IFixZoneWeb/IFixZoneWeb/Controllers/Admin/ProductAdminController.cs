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
    [Authorize(Roles = "Admin,Staff")]
    public class ProductAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductAdminController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ================== DANH SÁCH + TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG ==================
        public async Task<IActionResult> Index(
            string? q,
            string? status,
            string? sort,
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSpecifications)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.ProductName.Contains(q));

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status == status);

            // Sắp xếp
            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "stock_asc" => query.OrderBy(p => p.Stock),
                "stock_desc" => query.OrderByDescending(p => p.Stock),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var total = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Giữ state cho View
            ViewBag.Query = q;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;

            return View(products);
        }

        // ================== FORM THÊM / SỬA SẢN PHẨM ==================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.StatusList = new[] { "Active", "Inactive", "Hidden" };

            if (id == null || id == 0)
            {
                return View(new Product
                {
                    Status = "Active"
                });
            }

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
            ViewBag.StatusList = new[] { "Active", "Inactive", "Hidden" };

            // Bỏ validate navigation property
            ModelState.Remove("Category");

            if (!ModelState.IsValid)
                return View(model);

            // Upload ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadFolder = Path.Combine(_env.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadFolder);

                var ext = Path.GetExtension(imageFile.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await imageFile.CopyToAsync(stream);

                // Xóa ảnh cũ
                if (!string.IsNullOrEmpty(model.MainImage))
                {
                    var oldPath = Path.Combine(uploadFolder, model.MainImage);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                model.MainImage = fileName;
            }

            // Lưu DB
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

        // ================== XÓA / NGỪNG BÁN ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.OrderDetails)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            // Có dữ liệu liên quan → không xóa cứng
            if (product.OrderDetails.Any() || product.Reviews.Any())
            {
                product.Status = "Inactive";
                await _context.SaveChangesAsync();

                TempData["Warning"] = "Sản phẩm đã có dữ liệu liên quan → chuyển sang NGỪNG BÁN";
                return RedirectToAction(nameof(Index));
            }

            // Chưa phát sinh dữ liệu → xóa cứng
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
        public async Task<IActionResult> AddSpecification(
            int productId,
            string specName,
            string specValue)
        {
            if (string.IsNullOrWhiteSpace(specName) || string.IsNullOrWhiteSpace(specValue))
            {
                TempData["Error"] = "Tên thông số và giá trị không được để trống";
                return RedirectToAction(nameof(ManageSpecifications), new { productId });
            }

            var exists = await _context.Products.AnyAsync(p => p.ProductId == productId);
            if (!exists)
                return NotFound();

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
        public async Task<IActionResult> EditSpecification(
            int specId,
            string specName,
            string specValue)
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
