using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IFixZoneWeb.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        // ================= USER ID =================
        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // ================= TRANG TẤT CẢ THÔNG BÁO =================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = GetUserId();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // ================= DROPDOWN (TOP 10) =================
        [HttpGet]
        public async Task<IActionResult> List()
        {
            int userId = GetUserId();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            return PartialView("_NotificationList", notifications);
        }

        // ================= ĐẾM CHƯA ĐỌC =================
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            int userId = GetUserId();

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && n.IsRead == false);

            return Json(count);
        }

        // ================= ĐỌC 1 THÔNG BÁO =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int userId = GetUserId();

            var noti = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

            if (noti != null)
            {
                noti.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // ================= ĐỌC TẤT CẢ =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int userId = GetUserId();

            var list = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ToListAsync();

            foreach (var n in list)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
