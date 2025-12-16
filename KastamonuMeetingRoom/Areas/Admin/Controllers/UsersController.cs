using KastamonuMeetingRoom.Data;
using KastamonuMeetingRoom.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace KastamonuMeetingRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context; // ✅ DB context eklendi

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)   // ✅ ctor'a da eklendi
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;             // ✅ context atanıyor
        }

        // 📌 KULLANICI LİSTELEME
        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // 📌 ROLE ATA (GET)
        public async Task<IActionResult> EditRole(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = _roleManager.Roles.ToList();

            ViewBag.Roles = roles;
            return View(user);
        }

        // 📌 ROLE ATA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(string id, string roleName)
        {
            if (id == null || string.IsNullOrWhiteSpace(roleName))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles.ToArray());

            await _userManager.AddToRoleAsync(user, roleName);

            return RedirectToAction("Index");
        }

        // 📌 KULLANICI DETAY SAYFASI
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            // Kullanıcının rolleri
            var roles = await _userManager.GetRolesAsync(user);

            // Kullanıcının rezervasyonları
            var reservations = await _context.Reservations
                .Include(r => r.Room)
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.StartTime)
                .ToListAsync();

            ViewBag.Roles = roles;
            ViewBag.Reservations = reservations;

            return View(user);
        }
    }
}


