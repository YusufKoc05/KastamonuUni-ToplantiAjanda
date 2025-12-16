using KastamonuMeetingRoom.Data;
using KastamonuMeetingRoom.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


namespace KastamonuMeetingRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 📌 ODA LİSTELEME
        public IActionResult Index()
        {
            var rooms = _context.Rooms.ToList();
            return View(rooms);
        }

        // 📌 ODA EKLE (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 📌 ODA EKLE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Rooms.Add(room);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(room);
        }

        // 📌 ODA DÜZENLE (GET)
        public IActionResult Edit(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room == null)
                return NotFound();

            return View(room);
        }

        // 📌 ODA DÜZENLE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Room room)
        {
            if (ModelState.IsValid)
            {
                _context.Rooms.Update(room);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(room);
        }

        // 📌 ODA SİL (GET)
        public IActionResult Delete(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room == null)
                return NotFound();

            return View(room);
        }

        // 📌 ODA SİL (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
