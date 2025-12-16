using KastamonuMeetingRoom.Data;
using KastamonuMeetingRoom.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace KastamonuMeetingRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 📌 TÜM REZERVASYONLAR
        public IActionResult Index()
        {
            var reservations = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .OrderByDescending(r => r.StartTime)
                .ToList();

            return View(reservations);
        }

        // 📌 DETAY
        public IActionResult Details(int id)
        {
            var reservation = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationId == id);

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        // 📌 DÜZENLE (GET)
        public IActionResult Edit(int id)
        {
            var reservation = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationId == id);

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        // 📌 DÜZENLE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Reservation model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.Reservations.Update(model);
            _context.SaveChanges();

            return RedirectToAction("Details", new { id = model.ReservationId });
        }

        // 📌 İPTAL (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var reservation = _context.Reservations.Find(id);
            if (reservation == null)
                return NotFound();

            reservation.IsCancelled = true;
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // 📌 SİL (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var reservation = _context.Reservations.Find(id);
            if (reservation == null)
                return NotFound();

            _context.Reservations.Remove(reservation);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}


