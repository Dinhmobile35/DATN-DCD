using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFixZoneWeb.Controllers.Admin
{
    public class RoleManagementController : Controller
    {
        private readonly AppDbContext _context;

        public RoleManagementController(AppDbContext context)
        {
            _context = context;
        }

        // Danh sách người dùng + quyền
        public IActionResult Index()
        {
            var users = _context.Users
                .Include(x => x.Roles)
                .ToList();

            return View(users);
        }

        // Gán quyền (GET)
        public IActionResult Assign(int id)
        {
            var user = _context.Users
                .Include(x => x.Roles)
                .FirstOrDefault(x => x.UserId == id);

            if (user == null) return NotFound();

            ViewBag.Roles = _context.Roles.ToList();
            return View(user);
        }

        // Gán quyền (POST)
        [HttpPost]
        public IActionResult Assign(int id, List<int> roleIds)
        {
            var user = _context.Users
                .Include(x => x.Roles)
                .FirstOrDefault(x => x.UserId == id);

            if (user == null) return NotFound();

            user.Roles.Clear();

            if (roleIds != null)
            {
                var roles = _context.Roles.Where(r => roleIds.Contains(r.RoleId)).ToList();
                foreach (var r in roles)
                    user.Roles.Add(r);
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
