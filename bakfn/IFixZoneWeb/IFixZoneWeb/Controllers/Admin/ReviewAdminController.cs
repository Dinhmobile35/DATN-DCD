using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class ReviewAdminController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewAdminController(AppDbContext context)
        {
            _context = context;
        }

        /* ================== DANH SÁCH REVIEW ================== */
        public async Task<IActionResult> Index(int? productId, int? rating)
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .AsQueryable();

            // 🔍 Lọc theo sản phẩm
            if (productId.HasValue)
            {
                query = query.Where(r => r.ProductId == productId.Value);
            }

            // 🔍 Lọc theo rating
            if (rating.HasValue)
            {
                query = query.Where(r => r.Rating == rating.Value);
            }

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Dropdown sản phẩm
            ViewBag.Products = await _context.Products
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            ViewBag.SelectedProduct = productId;
            ViewBag.SelectedRating = rating;

            return View(reviews);
        }

        /* ================== XÓA REVIEW ================== */
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
