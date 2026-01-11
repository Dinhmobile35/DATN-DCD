using Microsoft.AspNetCore.Mvc;
using IFixZoneWeb.Models.Entities;
using Microsoft.EntityFrameworkCore;

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

        // LIST
        public IActionResult Index(string? q, int page = 1, int pageSize = 10)
        {
            var query = _context.Products
                .Include(x => x.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.ProductName.Contains(q));

            var total = query.Count();

            var products = query
                .OrderByDescending(x => x.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Query = q;

            return View(products);
        }

        // EDIT GET
        public IActionResult Edit(int? id)
        {
            ViewBag.Categories = _context.Categories.ToList();

            if (id == null)
                return View(new Product());

            var product = _context.Products
                .FirstOrDefault(x => x.ProductId == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // EDIT POST
        [HttpPost]
        public async Task<IActionResult> Edit(Product model, IFormFile? imageFile)
        {
            ViewBag.Categories = _context.Categories.ToList();

            // Bỏ validate cho navigation property
            ModelState.Remove("Category");

            if (!ModelState.IsValid)
                return View(model);

            var folder = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Upload ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(folder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await imageFile.CopyToAsync(stream);

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(model.MainImage))
                {
                    var old = Path.Combine(folder, model.MainImage);
                    if (System.IO.File.Exists(old))
                        System.IO.File.Delete(old);
                }

                model.MainImage = fileName;
            }

            if (model.ProductId == 0)
                _context.Products.Add(model);
            else
                _context.Products.Update(model);

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // DELETE
        public IActionResult Delete(int id)
        {
            var p = _context.Products.FirstOrDefault(x => x.ProductId == id);
            if (p == null) return NotFound();

            if (!string.IsNullOrEmpty(p.MainImage))
            {
                var path = Path.Combine(_env.WebRootPath, "images", "products", p.MainImage);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            _context.Products.Remove(p);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
