using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace IFixZoneWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
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
