using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IFixZoneWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Dashboard Admin
        public IActionResult Dashboard()
        {
            // Chặn nếu không phải Admin
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return Unauthorized();

            var model = new
            {
                Users = _context.Users.Count(),
                Products = _context.Products.Count(),
                Orders = _context.Orders.Count(),
                Revenue = _context.Orders
                    .Where(x => x.Status == "Hoàn thành")
                    .Sum(x => x.TotalAmount)
            };

            return View(model);
        }
    }
}
