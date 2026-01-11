using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFixZoneWeb.Controllers.Admin
{
    public class CategoryAdminController : Controller
    {
        private readonly AppDbContext _context;

        public CategoryAdminController(AppDbContext context)
        {
            _context = context;
        }

        // ============= LIST =============
        public IActionResult Index(string? q, int page = 1, int pageSize = 10)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.CategoryName.Contains(q));

            var total = query.Count();

            var data = query
                .OrderByDescending(x => x.CategoryId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Query = q;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;

            return View(data);
        }

        // ============= EDIT (GET) =============
        public IActionResult Edit(int? id)
        {
            if (id == null)
                return View(new Category());

            var c = _context.Categories.FirstOrDefault(x => x.CategoryId == id);
            if (c == null) return NotFound();

            return View(c);
        }

        // ============= EDIT (POST) =============
        [HttpPost]
        public IActionResult Edit(Category model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.CategoryId == 0)
                _context.Categories.Add(model);
            else
                _context.Categories.Update(model);

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // ============= DELETE =============
        public IActionResult Delete(int id)
        {
            var c = _context.Categories.FirstOrDefault(x => x.CategoryId == id);
            if (c == null) return NotFound();

            _context.Categories.Remove(c);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
