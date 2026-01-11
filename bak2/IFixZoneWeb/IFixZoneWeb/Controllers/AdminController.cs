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

        // ================== DASHBOARD ==================
        public IActionResult Dashboard()
        {
            // Lấy ROLE từ Session
            var role = HttpContext.Session.GetString("Role");

            // Nếu không phải Admin -> chặn
            if (string.IsNullOrEmpty(role) || role != "Admin")
                return RedirectToAction("Login", "Account");

            var model = new
            {
                Users = _context.Users.Count(),
                Products = _context.Products.Count(),
                Orders = _context.Orders.Count(),
                Revenue = _context.Orders
                    .Where(x => x.Status == "Hoàn thành")
                    .Sum(x => (decimal?)x.TotalAmount) ?? 0
            };

            return View(model);
        }
    }
}
