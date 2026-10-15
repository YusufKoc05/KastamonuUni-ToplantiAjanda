using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using KastamonuMeetingRoom.Data;
using KastamonuMeetingRoom.Hubs;
using KastamonuMeetingRoom.Models;
using KastamonuMeetingRoom.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KastamonuMeetingRoom.Controllers
{
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly CalendarIntegrationService _calendarService;
        private readonly IHubContext<CalendarHub> _hubContext;

        public ReservationController(
            ApplicationDbContext context,
            IEmailSender emailSender,
            CalendarIntegrationService calendarService,
            IHubContext<CalendarHub> hubContext)
        {
            _context = context;
            _emailSender = emailSender;
            _calendarService = calendarService;
            _hubContext = hubContext;
        }

        // 📌 1) REZERVASYONLARIM LİSTESİ
        [Authorize]
        public IActionResult Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            var reservations = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .AsQueryable();

            if (!isAdmin && userId != null)
            {
                reservations = reservations.Where(r => r.UserId == userId);
            }

            var list = reservations.OrderByDescending(r => r.StartTime).ToList();
            return View(list);
        }

        // 📌 2) REZERVASYON DETAYI & AJANDA ENTEGRASYONU
        [AllowAnonymous]
        public IActionResult Details(int id)
        {
            var reservation = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationId == id);

            if (reservation == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            ViewBag.CanEdit = User.Identity?.IsAuthenticated == true && (isAdmin || reservation.UserId == userId);
            ViewBag.GoogleCalendarUrl = _calendarService.GetGoogleCalendarUrl(reservation);
            ViewBag.OutlookCalendarUrl = _calendarService.GetOutlookCalendarUrl(reservation);

            return View(reservation);
        }

        // 📌 3) REZERVASYON OLUŞTUR (GET)
        [Authorize]
        public IActionResult Create(int? roomId)
        {
            ViewBag.Rooms = _context.Rooms.OrderBy(r => r.Name).ToList();

            if (User.IsInRole("Admin"))
                ViewBag.Users = _context.Users.OrderBy(u => u.FullName).ToList();

            var defaultStart = DateTime.Today.AddDays(1).AddHours(10);
            var model = new Reservation
            {
                RoomId = roomId ?? 0,
                StartTime = defaultStart,
                EndTime = defaultStart.AddHours(1),
                MeetingType = "Akademik Kurul Toplantısı"
            };

            return View(model);
        }

        // 📌 4) REZERVASYON OLUŞTUR (POST)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reservation reservation)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ModelState.Remove("Room");
            ModelState.Remove("User");

            if (!User.IsInRole("Admin"))
            {
                reservation.UserId = userId;
                ModelState.Remove("UserId");
            }

            if (reservation.EndTime <= reservation.StartTime)
                ModelState.AddModelError("", "Bitiş saati başlangıç saatinden sonra olmalıdır!");

            var duration = reservation.EndTime - reservation.StartTime;

            if (duration.TotalMinutes < 30)
                ModelState.AddModelError("", "Toplantı süresi en az 30 dakika olmalıdır!");

            if (duration.TotalHours > 8)
                ModelState.AddModelError("", "Toplantı süresi en fazla 8 saat olabilir!");

            if (!ModelState.IsValid)
            {
                ViewBag.Rooms = _context.Rooms.OrderBy(r => r.Name).ToList();
                if (User.IsInRole("Admin"))
                    ViewBag.Users = _context.Users.OrderBy(u => u.FullName).ToList();
                return View(reservation);
            }

            // Çakışma kontrolü
            var conflict = _context.Reservations
                .Where(r => r.RoomId == reservation.RoomId &&
                            !r.IsCancelled &&
                            (
                                (reservation.StartTime >= r.StartTime && reservation.StartTime < r.EndTime) ||
                                (reservation.EndTime > r.StartTime && reservation.EndTime <= r.EndTime) ||
                                (reservation.StartTime <= r.StartTime && reservation.EndTime >= r.EndTime)
                            ))
                .FirstOrDefault();

            if (conflict != null)
            {
                ModelState.AddModelError("", $"Seçtiğiniz saatlerde bu salonda '{conflict.Title}' başlıklı başka bir toplantı planlanmıştır. Lütfen farklı bir saat veya salon seçiniz.");
                ViewBag.Rooms = _context.Rooms.OrderBy(r => r.Name).ToList();
                if (User.IsInRole("Admin"))
                    ViewBag.Users = _context.Users.OrderBy(u => u.FullName).ToList();
                return View(reservation);
            }

            reservation.CreatedAt = DateTime.Now;
            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            // ⚡ SignalR: Canlı takvim güncelleme bildirimi yayınla
            await _hubContext.Clients.All.SendAsync("ReceiveCalendarUpdate", "created", reservation.ReservationId, reservation.Title);

            // E-posta Bildirimi
            reservation = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationId == reservation.ReservationId);

            if (reservation?.User?.Email != null)
            {
                try
                {
                    await _emailSender.SendEmailAsync(
                        reservation.User.Email,
                        "Kastamonu Üniversitesi Toplantı Rezervasyonu Onayı",
                        $@"
<div style='font-family:Arial,sans-serif; color:#222; line-height:1.6;'>
    <h2 style='color:#b22525;'>Kastamonu Üniversitesi Toplantı ve Ajanda Sistemi</h2>
    <p>Toplantı rezervasyonunuz başarıyla kaydedilmiştir.</p>
    <table style='border-collapse:collapse; width:100%; max-width:500px;'>
        <tr><td style='padding:8px; border-bottom:1px solid #ddd;'><b>Toplantı:</b></td><td style='padding:8px; border-bottom:1px solid #ddd;'>{reservation.Title}</td></tr>
        <tr><td style='padding:8px; border-bottom:1px solid #ddd;'><b>Tür:</b></td><td style='padding:8px; border-bottom:1px solid #ddd;'>{reservation.MeetingType}</td></tr>
        <tr><td style='padding:8px; border-bottom:1px solid #ddd;'><b>Salon:</b></td><td style='padding:8px; border-bottom:1px solid #ddd;'>{reservation.Room?.Name} ({reservation.Room?.Location})</td></tr>
        <tr><td style='padding:8px; border-bottom:1px solid #ddd;'><b>Başlangıç:</b></td><td style='padding:8px; border-bottom:1px solid #ddd;'>{reservation.StartTime:dd.MM.yyyy HH:mm}</td></tr>
        <tr><td style='padding:8px; border-bottom:1px solid #ddd;'><b>Bitiş:</b></td><td style='padding:8px; border-bottom:1px solid #ddd;'>{reservation.EndTime:dd.MM.yyyy HH:mm}</td></tr>
    </table>
    <p style='color:#b22525; font-weight:bold; margin-top:15px;'>İyi çalışmalar dileriz.</p>
</div>"
                    );
                }
                catch
                {
                }
            }

            return RedirectToAction("Details", new { id = reservation.ReservationId });
        }

        // 📌 ⚡ HIZLI MODAL REZERVASYON API (Takvim üzerinden doğrudan kaydetme)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> QuickCreate([FromBody] QuickReservationModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Title) || model.RoomId <= 0)
                return Json(new { success = false, message = "Lütfen toplantı başlığı ve salon seçiniz!" });

            if (model.EndTime <= model.StartTime)
                return Json(new { success = false, message = "Bitiş saati başlangıç saatinden sonra olmalıdır!" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Çakışma kontrolü
            var conflict = _context.Reservations
                .Where(r => r.RoomId == model.RoomId &&
                            !r.IsCancelled &&
                            (
                                (model.StartTime >= r.StartTime && model.StartTime < r.EndTime) ||
                                (model.EndTime > r.StartTime && model.EndTime <= r.EndTime) ||
                                (model.StartTime <= r.StartTime && model.EndTime >= r.EndTime)
                            ))
                .FirstOrDefault();

            if (conflict != null)
                return Json(new { success = false, message = $"Bu saat aralığında salonda '{conflict.Title}' başlıklı başka bir toplantı bulunmaktadır!" });

            var reservation = new Reservation
            {
                RoomId = model.RoomId,
                UserId = userId,
                Title = model.Title,
                MeetingType = model.MeetingType ?? "Genel Toplantı",
                Description = model.Description,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                CreatedAt = DateTime.Now
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // SignalR yayını
            await _hubContext.Clients.All.SendAsync("ReceiveCalendarUpdate", "created", reservation.ReservationId, reservation.Title);

            return Json(new { success = true, reservationId = reservation.ReservationId });
        }

        // 📌 🖱️ SÜRÜKLE-BIRAK / SAAT UZATMA GÜNCELLEME API (FullCalendar Drag & Drop / Resize)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> QuickUpdateTimes([FromBody] QuickUpdateModel model)
        {
            var reservation = await _context.Reservations.FindAsync(model.Id);
            if (reservation == null)
                return Json(new { success = false, message = "Toplantı bulunamadı!" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && reservation.UserId != userId)
                return Json(new { success = false, message = "Bu toplantının saatlerini değiştirme yetkiniz yok!" });

            if (reservation.IsCancelled)
                return Json(new { success = false, message = "İptal edilmiş bir toplantı güncellenemez!" });

            if (model.End <= model.Start)
                return Json(new { success = false, message = "Bitiş saati başlangıç saatinden sonra olmalıdır!" });

            // Çakışma kontrolü (kendisi hariç)
            var conflict = _context.Reservations
                .Where(r => r.RoomId == reservation.RoomId &&
                            !r.IsCancelled &&
                            r.ReservationId != reservation.ReservationId &&
                            (
                                (model.Start >= r.StartTime && model.Start < r.EndTime) ||
                                (model.End > r.StartTime && model.End <= r.EndTime) ||
                                (model.Start <= r.StartTime && model.End >= r.EndTime)
                            ))
                .FirstOrDefault();

            if (conflict != null)
                return Json(new { success = false, message = $"Yeni saat aralığı '{conflict.Title}' toplantısı ile çakışıyor! İşlem geri alındı." });

            reservation.StartTime = model.Start;
            reservation.EndTime = model.End;
            await _context.SaveChangesAsync();

            // SignalR yayını
            await _hubContext.Clients.All.SendAsync("ReceiveCalendarUpdate", "updated", reservation.ReservationId, reservation.Title);

            return Json(new { success = true });
        }

        // 📌 5) REZERVASYON DÜZENLE (GET)
        [Authorize]
        public IActionResult Edit(int id)
        {
            var reservation = _context.Reservations.Find(id);
            if (reservation == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!User.IsInRole("Admin") && reservation.UserId != userId)
                return Unauthorized();

            ViewBag.Rooms = _context.Rooms.OrderBy(r => r.Name).ToList();
            return View(reservation);
        }

        // 📌 6) REZERVASYON DÜZENLE (POST)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Reservation reservation)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var original = _context.Reservations
                .AsNoTracking()
                .FirstOrDefault(r => r.ReservationId == reservation.ReservationId);

            if (original == null)
                return NotFound();

            if (!User.IsInRole("Admin") && original.UserId != userId)
                return Unauthorized();

            ModelState.Remove("Room");
            ModelState.Remove("User");

            reservation.UserId = original.UserId;

            if (reservation.EndTime <= reservation.StartTime)
            {
                ModelState.AddModelError("", "Bitiş saati başlangıç saatinden sonra olmalıdır!");
                ViewBag.Rooms = _context.Rooms.OrderBy(r => r.Name).ToList();
                return View(reservation);
            }

            var conflict = _context.Reservations
                .Where(r => r.RoomId == reservation.RoomId &&
                            !r.IsCancelled &&
                            r.ReservationId != reservation.ReservationId &&
                            (
                                (reservation.StartTime >= r.StartTime && reservation.StartTime < r.EndTime) ||
                                (reservation.EndTime > r.StartTime && reservation.EndTime <= r.EndTime) ||
                                (reservation.StartTime <= r.StartTime && reservation.EndTime >= r.EndTime)
                            ))
                .FirstOrDefault();

            if (conflict != null)
            {
                ModelState.AddModelError("", "Seçilen saatlerde salon zaten rezerve edilmiştir!");
                ViewBag.Rooms = _context.Rooms.OrderBy(r => r.Name).ToList();
                return View(reservation);
            }

            if (ModelState.IsValid)
            {
                _context.Reservations.Update(reservation);
                _context.SaveChanges();

                await _hubContext.Clients.All.SendAsync("ReceiveCalendarUpdate", "updated", reservation.ReservationId, reservation.Title);
                return RedirectToAction("Details", new { id = reservation.ReservationId });
            }

            ViewBag.Rooms = _context.Rooms.OrderBy(r => r.Name).ToList();
            return View(reservation);
        }

        // 📌 7) AJANDA VE TAKVİM SAYFASI
        [AllowAnonymous]
        public IActionResult Calendar(int? roomId)
        {
            ViewBag.Rooms = _context.Rooms.OrderBy(r => r.Name).ToList();
            ViewBag.SelectedRoomId = roomId;
            ViewBag.MeetingTypes = new[] {
                "Akademik Kurul Toplantısı",
                "Tez Savunması (Y.Lisans / Doktora)",
                "Senato Toplantısı",
                "BAP / Proje Toplantısı",
                "İdari Koordinasyon Toplantısı",
                "Konferans & Seminer",
                "Öğrenci Kulübü Etkinliği"
            };
            return View();
        }

        // 📌 8) REZERVASYON SİL
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var reservation = _context.Reservations.Find(id);
            if (reservation == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!User.IsInRole("Admin") && reservation.UserId != userId)
                return Unauthorized();

            _context.Reservations.Remove(reservation);
            _context.SaveChanges();

            await _hubContext.Clients.All.SendAsync("ReceiveCalendarUpdate", "deleted", id, reservation.Title);
            return RedirectToAction("Index");
        }

        // 📌 9) REZERVASYON İPTAL
        [Authorize]
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

            await _hubContext.Clients.All.SendAsync("ReceiveCalendarUpdate", "cancelled", id, reservation.Title);

            if (reservation.User?.Email != null)
            {
                try
                {
                    await _emailSender.SendEmailAsync(
                        reservation.User.Email,
                        "Kastamonu Üniversitesi - Toplantı İptal Bildirimi",
                        $@"
<div style='font-family:Arial,sans-serif; color:#222; line-height:1.6;'>
    <h3 style='color:#b22525;'>Toplantı İptal Edildi</h3>
    <p><b>Toplantı:</b> {reservation.Title}</p>
    <p><b>Salon:</b> {reservation.Room?.Name}</p>
    <p><b>Zaman:</b> {reservation.StartTime:dd.MM.yyyy HH:mm} - {reservation.EndTime:HH:mm}</p>
</div>"
                    );
                }
                catch
                {
                }
            }

            return RedirectToAction("Index");
        }

        // 📌 10) FullCalendar JSON API
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetEvents(int? roomId, string? meetingType)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            var query = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .AsQueryable();

            if (roomId.HasValue && roomId.Value > 0)
            {
                query = query.Where(r => r.RoomId == roomId.Value);
            }

            if (!string.IsNullOrEmpty(meetingType))
            {
                query = query.Where(r => r.MeetingType == meetingType);
            }

            var events = query.Select(r => new
            {
                id = r.ReservationId,
                title = r.Title,
                start = r.StartTime.ToString("s"),
                end = r.EndTime.ToString("s"),
                editable = !r.IsCancelled && !string.IsNullOrEmpty(userId) && (r.UserId == userId || isAdmin),
                color = r.IsCancelled ? "#9ca3af" : (r.UserId == userId ? "#111827" : (r.MeetingType == "Senato Toplantısı" ? "#7f1d1d" : "#b22525")),
                textColor = "#ffffff",
                extendedProps = new
                {
                    room = r.Room != null ? r.Room.Name : "Bilinmiyor",
                    location = r.Room != null ? r.Room.Location : "",
                    capacity = r.Room != null ? r.Room.Capacity : 0,
                    user = r.User != null ? r.User.FullName : "Akademisyen / Personel",
                    meetingType = r.MeetingType ?? "Genel Toplantı",
                    description = r.Description ?? "Gündem maddesi eklenmedi.",
                    isCancelled = r.IsCancelled,
                    isMine = !string.IsNullOrEmpty(userId) && (r.UserId == userId || isAdmin),
                    startTimeText = r.StartTime.ToString("dd.MM.yyyy HH:mm"),
                    endTimeText = r.EndTime.ToString("HH:mm"),
                    icsUrl = $"/Reservation/DownloadIcs/{r.ReservationId}",
                    detailsUrl = $"/Reservation/Details/{r.ReservationId}"
                }
            }).ToList();

            return Json(events);
        }

        // 📌 11) iCalendar (.ics) İndir
        [HttpGet]
        [AllowAnonymous]
        public IActionResult DownloadIcs(int id)
        {
            var reservation = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationId == id);

            if (reservation == null)
                return NotFound();

            var icsContent = _calendarService.GenerateIcs(reservation);
            var safeTitle = string.Concat(reservation.Title.Where(c => !System.IO.Path.GetInvalidFileNameChars().Contains(c)));
            if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = "Toplanti";

            var fileName = $"KU_Toplanti_{safeTitle}_{reservation.StartTime:yyyyMMdd}.ics";
            var bytes = Encoding.UTF8.GetBytes(icsContent);

            return File(bytes, "text/calendar; charset=utf-8", fileName);
        }

        // 📌 12) Kişisel Tüm Ajandayı İndir
        [HttpGet]
        [Authorize]
        public IActionResult ExportMyCalendarIcs()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            var query = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Where(r => !r.IsCancelled);

            if (!isAdmin && userId != null)
            {
                query = query.Where(r => r.UserId == userId);
            }

            var list = query.OrderBy(r => r.StartTime).ToList();
            var icsContent = _calendarService.GenerateMultipleIcs(list, "Kastamonu Üniversitesi - Kişisel Ajandam");
            var bytes = Encoding.UTF8.GetBytes(icsContent);

            return File(bytes, "text/calendar; charset=utf-8", "Kastamonu_Universitesi_Ajandam.ics");
        }

        // 📌 13) Salon Ajandasını İndir
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExportRoomIcs(int roomId)
        {
            var room = _context.Rooms.Find(roomId);
            if (room == null)
                return NotFound();

            var reservations = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Where(r => r.RoomId == roomId && !r.IsCancelled)
                .OrderBy(r => r.StartTime)
                .ToList();

            var icsContent = _calendarService.GenerateMultipleIcs(reservations, $"{room.Name} - Toplantı Ajandası");
            var bytes = Encoding.UTF8.GetBytes(icsContent);

            var safeRoomName = string.Concat(room.Name.Where(c => !System.IO.Path.GetInvalidFileNameChars().Contains(c)));
            return File(bytes, "text/calendar; charset=utf-8", $"{safeRoomName}_Ajandasi.ics");
        }
    }

    public class QuickUpdateModel
    {
        public int Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class QuickReservationModel
    {
        public int RoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? MeetingType { get; set; }
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
