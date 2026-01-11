using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFixZoneWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ================== LOGIN ==================
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users
                .Include(u => u.Roles)                // 🔹 lấy luôn danh sách quyền
                .FirstOrDefault(x =>
                    x.Username == model.Username &&
                    x.PasswordHash == model.Password);

            if (user == null)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
                return View(model);
            }

            var roles = user.Roles.Select(r => r.RoleName).ToList();
            var role = roles.FirstOrDefault() ?? "Customer";

            // 🔹 Lưu session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetString("Role", role);

            TempData["LoginSuccess"] = $"Xin chào {user.FullName}";

            // 🔹 Điều hướng theo quyền
            if (roles.Contains("Admin"))
                return RedirectToAction("Dashboard", "Admin");

            if (roles.Contains("Staff"))
                return RedirectToAction("ManageOrders", "Admin");

            return RedirectToAction("Index", "Home");
        }


        // ================== REGISTER ==================
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_context.Users.Any(x => x.Username == model.Username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại";
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName,
                Username = model.Username,
                Email = model.Email,
                PasswordHash = model.Password,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            // 🔹 Gán mặc định quyền Customer (qua navigation)
            var customerRole = _context.Roles.First(r => r.RoleName == "Customer");
            user.Roles.Add(customerRole);

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["RegisterSuccess"] = "Đăng ký thành công! Hãy đăng nhập.";
            return RedirectToAction("Login");
        }


        // ================== PROFILE ==================
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            var user = _context.Users
                .Include(u => u.Roles)
                .FirstOrDefault(x => x.UserId == userId);

            if (user == null)
                return RedirectToAction("Login");

            return View(user);
        }


        // ================== LOGOUT ==================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
