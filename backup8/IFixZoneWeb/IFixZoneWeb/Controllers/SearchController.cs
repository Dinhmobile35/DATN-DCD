using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFixZoneWeb.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        // 🔎 Realtime search
        [HttpGet]
        public async Task<IActionResult> Suggest(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new List<object>());

            var products = await _context.Products
                .Where(p => p.Status == "Active"
                         && p.ProductName.Contains(q))
                .OrderByDescending(p => p.Stock)
                .Take(8)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Price,
                    p.MainImage
                })
                .ToListAsync();

            return Json(products);
        }
    }
}
