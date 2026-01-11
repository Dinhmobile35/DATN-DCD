using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ========== TRANG ĐĂNG NHẬP ==========
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var user = _context.Users
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.Username == model.Username);

            if (user == null || user.PasswordHash != model.Password)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var role = user.Roles.FirstOrDefault()?.RoleName ?? "Customer";

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
            HttpContext.Session.SetString("Role", role);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Staff" => RedirectToAction("Index", "OrderAdmin"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // ========== TRANG CÁ NHÂN ==========
        [HttpGet]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login");

            var user = _context.Users
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.UserId == userId.Value);

            if (user == null)
                return RedirectToAction("Login");

            return View(user);
        }

        // ========== CHỈNH SỬA HỒ SƠ (không bao gồm đổi mật khẩu) ==========
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login");

            var model = new EditProfileViewModel
            {
                Id = user.UserId.ToString(),
                FullName = user.FullName,
                Phone = user.Phone,
                Address = user.Address
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || userId.Value.ToString() != model.Id)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            user.FullName = model.FullName;
            user.Phone = model.Phone;
            user.Address = model.Address;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        // ========== ĐỔI MẬT KHẨU RIÊNG (mới thêm) ==========
        [HttpGet]
        public IActionResult ChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return RedirectToAction("Login");

            // Kiểm tra mật khẩu hiện tại
            if (user.PasswordHash != model.CurrentPassword)
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                return View(model);
            }

            // Cập nhật mật khẩu mới
            user.PasswordHash = model.NewPassword; // Nên hash lại (dùng BCrypt hoặc Identity)

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        // ========== ĐĂNG XUẤT ==========
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ========== KIỂM TRA ĐĂNG NHẬP (AJAX) ==========
        [HttpGet]
        public JsonResult IsLoggedIn()
        {
            bool isLoggedIn = HttpContext.Session.GetInt32("UserId").HasValue;
            return Json(new { isLoggedIn });
        }
    }
}