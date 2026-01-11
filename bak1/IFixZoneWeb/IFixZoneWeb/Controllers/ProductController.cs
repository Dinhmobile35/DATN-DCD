using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFixZoneWeb.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _context.Products.Take(20).ToList();
            return View(products);
        }

        // 👉 DETAILS
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductSpecifications)
                .Include(p => p.Reviews)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            return View(product);
        }
    }
}
