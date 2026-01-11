using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IFixZoneWeb.Controllers.Admin
{
    public class UserAdminController : Controller
    {
        private readonly AppDbContext _context;

        public UserAdminController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _context.Users
                .Include(x => x.Roles)
                .OrderByDescending(x => x.UserId)
                .ToList();

            return View(users);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null)
                return View(new User());

            var user = _context.Users.FirstOrDefault(x => x.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(User model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.UserId == 0)
                _context.Users.Add(model);
            else
                _context.Users.Update(model);

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var user = _context.Users.FirstOrDefault(x => x.UserId == id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
