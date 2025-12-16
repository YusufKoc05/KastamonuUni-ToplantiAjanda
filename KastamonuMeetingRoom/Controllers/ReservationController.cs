using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KastamonuMeetingRoom.Data;
using KastamonuMeetingRoom.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace KastamonuMeetingRoom.Controllers
{
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public ReservationController(ApplicationDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // 📌 1) REZERVASYON LİSTELEME
        public IActionResult Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            var reservations = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .AsQueryable();

            if (!isAdmin && userId != null)
                reservations = reservations.Where(r => r.UserId == userId);

            return View(reservations.ToList());
        }

        // 📌 2) REZERVASYON OLUŞTUR (GET)
        public IActionResult Create()
        {
            ViewBag.Rooms = _context.Rooms.ToList();

            if (User.IsInRole("Admin"))
                ViewBag.Users = _context.Users.ToList();

            return View();
        }

        // 📌 3) REZERVASYON OLUŞTUR (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation reservation)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // ❗ Navigation property hatalarını engelle
            ModelState.Remove("Room");
            ModelState.Remove("User");

            // ❗ Admin değilse UserId otomatik ata
            if (!User.IsInRole("Admin"))
            {
                reservation.UserId = userId;
                ModelState.Remove("UserId");
            }

            // ⏱ Süre kontrolü
            if (reservation.EndTime <= reservation.StartTime)
                ModelState.AddModelError("", "Bitiş zamanı başlangıçtan sonra olmalıdır!");

            var duration = reservation.EndTime - reservation.StartTime;

            if (duration.TotalMinutes < 30)
                ModelState.AddModelError("", "Rezervasyon en az 30 dakika olmalıdır!");

            if (duration.TotalHours > 6)
                ModelState.AddModelError("", "Rezervasyon en fazla 6 saat olabilir!");

            if (!ModelState.IsValid)
            {
                ViewBag.Rooms = _context.Rooms.ToList();
                if (User.IsInRole("Admin"))
                    ViewBag.Users = _context.Users.ToList();
                return View(reservation);
            }

            // 🔄 ÇAKIŞMA KONTROLÜ
            var conflict = _context.Reservations
                .Where(r => r.RoomId == reservation.RoomId &&
                            (
                                (reservation.StartTime >= r.StartTime && reservation.StartTime < r.EndTime) ||
                                (reservation.EndTime > r.StartTime && reservation.EndTime <= r.EndTime) ||
                                (reservation.StartTime <= r.StartTime && reservation.EndTime >= r.EndTime)
                            ))
                .FirstOrDefault();

            if (conflict != null)
            {
                ModelState.AddModelError("", "Bu saatlerde oda zaten rezerve edilmiş!");
                ViewBag.Rooms = _context.Rooms.ToList();
                if (User.IsInRole("Admin"))
                    ViewBag.Users = _context.Users.ToList();
                return View(reservation);
            }

            // ✔ KAYDET
            reservation.CreatedAt = DateTime.Now;
            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            // ✔ Mail için yeniden yükle
            reservation = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationId == reservation.ReservationId);

            if (reservation.User?.Email != null)
            {
                await _emailSender.SendEmailAsync(
                    reservation.User.Email,
                    "Rezervasyon Oluşturuldu",
                    $@"
<h3>Rezervasyon Onayınız</h3>
<p><b>Oda:</b> {reservation.Room.Name}</p>
<p><b>Başlangıç:</b> {reservation.StartTime}</p>
<p><b>Bitiş:</b> {reservation.EndTime}</p>
<p>İyi toplantılar dileriz 🌿</p>"
                );
            }

            return RedirectToAction("Index");
        }

        // 📌 4) REZERVASYON DÜZENLE (GET)
        public IActionResult Edit(int id)
        {
            var reservation = _context.Reservations.Find(id);
            if (reservation == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!User.IsInRole("Admin") && reservation.UserId != userId)
                return Unauthorized();

            ViewBag.Rooms = _context.Rooms.ToList();
            return View(reservation);
        }

        // 📌 5) REZERVASYON DÜZENLE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Reservation reservation)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var original = _context.Reservations
                .AsNoTracking()
                .FirstOrDefault(r => r.ReservationId == reservation.ReservationId);

            if (original == null)
                return NotFound();

            // ❗ Navigation türlerini sil
            ModelState.Remove("Room");
            ModelState.Remove("User");

            // ❗ Admin değilse UserId değişmesin
            reservation.UserId = original.UserId;

            // Süre kontrolü
            if (reservation.EndTime <= reservation.StartTime)
            {
                ModelState.AddModelError("", "Bitiş zamanı başlangıçtan sonra olmalıdır!");
                ViewBag.Rooms = _context.Rooms.ToList();
                return View(reservation);
            }

            var duration = reservation.EndTime - reservation.StartTime;

            if (duration.TotalMinutes < 30)
            {
                ModelState.AddModelError("", "Rezervasyon süresi en az 30 dakika olmalıdır!");
                ViewBag.Rooms = _context.Rooms.ToList();
                return View(reservation);
            }

            if (duration.TotalHours > 6)
            {
                ModelState.AddModelError("", "Rezervasyon süresi en fazla 6 saat olabilir!");
                ViewBag.Rooms = _context.Rooms.ToList();
                return View(reservation);
            }

            // Çakışma kontrolü (kendisi hariç)
            var conflict = _context.Reservations
                .Where(r => r.RoomId == reservation.RoomId &&
                            r.ReservationId != reservation.ReservationId &&
                            (
                                (reservation.StartTime >= r.StartTime && reservation.StartTime < r.EndTime) ||
                                (reservation.EndTime > r.StartTime && reservation.EndTime <= r.EndTime) ||
                                (reservation.StartTime <= r.StartTime && reservation.EndTime >= r.EndTime)
                            ))
                .FirstOrDefault();

            if (conflict != null)
            {
                ModelState.AddModelError("", "Bu saatlerde oda zaten rezerve edilmiş!");
                ViewBag.Rooms = _context.Rooms.ToList();
                return View(reservation);
            }

            if (ModelState.IsValid)
            {
                _context.Reservations.Update(reservation);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Rooms = _context.Rooms.ToList();
            return View(reservation);
        }

        // 📌 6) TAKVİM SAYFASI
        public IActionResult Calendar()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            var reservations = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .AsQueryable();

            if (!isAdmin && userId != null)
                reservations = reservations.Where(r => r.UserId == userId);

            return View(reservations.ToList());
        }

        // 📌 7) REZERVASYON SİL
        public IActionResult Delete(int id)
        {
            var reservation = _context.Reservations.Find(id);
            if (reservation == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!User.IsInRole("Admin") && reservation.UserId != userId)
                return Unauthorized();

            _context.Reservations.Remove(reservation);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // 📌 8) REZERVASYON İPTAL
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationId == id);

            if (reservation == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!User.IsInRole("Admin") && reservation.UserId != userId)
                return Unauthorized();

            reservation.IsCancelled = true;
            _context.Reservations.Update(reservation);
            _context.SaveChanges();

            if (reservation.User?.Email != null)
            {
                await _emailSender.SendEmailAsync(
                    reservation.User.Email,
                    "Rezervasyon İptal Edildi",
                    $@"
<h3>Rezervasyon İptal Edildi</h3>
<p><b>Oda:</b> {reservation.Room.Name}</p>
<p><b>Başlangıç:</b> {reservation.StartTime}</p>
<p><b>Bitiş:</b> {reservation.EndTime}</p>
<p>Eğer bu işlemi siz yapmadıysanız lütfen sistem yöneticisine haber verin.</p>"
                );
            }

            return RedirectToAction("Index");
        }

        // 📌 9) FullCalendar API
        [HttpGet]
        public IActionResult GetEvents()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            var reservations = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .AsQueryable();

            if (!isAdmin && userId != null)
                reservations = reservations.Where(r => r.UserId == userId);

            var events = reservations.Select(r => new
            {
                id = r.ReservationId,
                title = r.Title, // 👈 SADECE "Sunum"
                start = r.StartTime,
                end = r.EndTime,
                color = r.IsCancelled ? "#9ca3af" : "#991b1b",
                extendedProps = new
                {
                    room = r.Room.Name,
                    user = r.User.FullName,   // 👈 KULLANICI ADI
                    description = r.Description ?? "Açıklama yok",
                    isCancelled = r.IsCancelled
                }
            }).ToList();

            return Json(events);
        }





        public IActionResult GetCalendarEvents()
        {
            var events = _context.Reservations
                .Include(r => r.Room)
                .Select(r => new
                {
                    id = r.ReservationId,
                    title = r.Room.Name + " - " + r.Title,
                    start = r.StartTime,
                    end = r.EndTime,
                    isCancelled = r.IsCancelled
                })
                .ToList();

            return Json(events);
        }

    }
}






