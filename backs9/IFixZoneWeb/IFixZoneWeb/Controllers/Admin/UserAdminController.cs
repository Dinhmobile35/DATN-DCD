using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace IFixZoneWeb.Controllers.Admin
{
    [Authorize(Roles = "Admin")] // 🔐 CHỈ ADMIN MỚI ĐƯỢC TRUY CẬP
    public class UserAdminController : Controller
    {
        private readonly AppDbContext _context;

        public UserAdminController(AppDbContext context)
        {
            _context = context;
        }

        // ================== DANH SÁCH USER ==================
        public IActionResult Index()
        {
            var users = _context.Users
                .Include(u => u.Roles)
                .OrderByDescending(u => u.UserId)
                .ToList();

            return View(users);
        }

        // ================== TẠO / SỬA USER ==================
        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                // Tạo mới
                return View(new User
                {
                    IsActive = true
                });
            }

            var user = _context.Users
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.UserId == id);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // ================== LƯU USER ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User model, string? newPassword)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ================== TẠO USER ==================
            if (model.UserId == 0)
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    ModelState.AddModelError("", "Vui lòng nhập mật khẩu");
                    return View(model);
                }

                model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                model.CreatedAt = DateTime.Now;

                _context.Users.Add(model);
            }
            // ================== CẬP NHẬT USER ==================
            else
            {
                var userInDb = _context.Users.FirstOrDefault(u => u.UserId == model.UserId);
                if (userInDb == null)
                    return NotFound();

                // Cập nhật thông tin
                userInDb.FullName = model.FullName;
                userInDb.Email = model.Email;
                userInDb.Phone = model.Phone;
                userInDb.Address = model.Address;
                userInDb.IsActive = model.IsActive;

                // 🔒 CHỈ HASH LẠI KHI ADMIN NHẬP PASSWORD MỚI
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    userInDb.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // ================== XÓA USER ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
                return NotFound();

            // ❗ KHÔNG XÓA CỨNG – CHỈ KHÓA
            user.IsActive = false;

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}
