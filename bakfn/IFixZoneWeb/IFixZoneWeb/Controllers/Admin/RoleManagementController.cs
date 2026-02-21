using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFixZoneWeb.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class RoleManagementController : Controller
    {
        private readonly AppDbContext _context;

        public RoleManagementController(AppDbContext context)
        {
            _context = context;
        }

        // ================== DANH SÁCH USER + ROLE ==================
        public IActionResult Index()
        {
            var users = _context.Users
                .Include(u => u.Roles)
                .OrderByDescending(u => u.UserId)
                .ToList();

            return View(users);
        }

        // ================== GÁN QUYỀN (GET) ==================
        [HttpGet]
        public IActionResult Assign(int id)
        {
            var user = _context.Users
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.UserId == id);

            if (user == null)
                return NotFound();

            ViewBag.AllRoles = _context.Roles.ToList();
            return View(user);
        }

        // ================== GÁN QUYỀN (POST) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Assign(int id, List<int> roleIds)
        {
            var user = _context.Users
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.UserId == id);

            if (user == null)
                return NotFound();

            if (roleIds == null || roleIds.Count == 0)
            {
                TempData["Error"] = "Người dùng phải có ít nhất 1 quyền";
                ViewBag.AllRoles = _context.Roles.ToList();
                return View(user);
            }

            // Xóa quyền cũ
            user.Roles.Clear();

            // Gán quyền mới
            var roles = _context.Roles
                .Where(r => roleIds.Contains(r.RoleId))
                .ToList();

            foreach (var role in roles)
            {
                user.Roles.Add(role);
            }

            _context.SaveChanges();
            TempData["Success"] = "Cập nhật quyền thành công";

            return RedirectToAction(nameof(Index));
        }
    }
}
