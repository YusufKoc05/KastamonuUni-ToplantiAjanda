using KastamonuMeetingRoom.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KastamonuMeetingRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 📌 BUGÜNKÜ REZERVASYON SAYISI
            ViewBag.TodayCount = _context.Reservations
                .Count(r => r.StartTime.Date == DateTime.Today);

            // 📌 TOPLAM REZERVASYON SAYISI
            ViewBag.TotalCount = _context.Reservations.Count();

            // 📌 KULLANICI SAYISI
            ViewBag.UserCount = _context.Users.Count();

            // 📌 TOPLAM ODA SAYISI
            ViewBag.TotalRooms = _context.Rooms.Count();

            // ----------------------------------------------------------
            // 🏆 EN ÇOK KULLANILAN ODALAR (TOP 5)
            // ----------------------------------------------------------
            ViewBag.TopRooms = _context.Reservations
                .Include(r => r.Room)
                .GroupBy(r => r.Room.Name)
                .Select(g => new
                {
                    RoomName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            // ----------------------------------------------------------
            // 📅 AYLIK REZERVASYON TRENDİ (GRAFİK)
            // ----------------------------------------------------------
            ViewBag.MonthlyData = _context.Reservations
                .GroupBy(r => new { r.StartTime.Year, r.StartTime.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            // ----------------------------------------------------------
            // 📊 GÜN BAZLI REZERVASYON DAĞILIMI (PASTA GRAFİĞİ)
            // ----------------------------------------------------------
        
            // ----------------------------------------------------------
            // 🕒 SAAT BAZLI REZERVASYON YOĞUNLUĞU (Bar Chart için)
            // ----------------------------------------------------------
     

            // ----------------------------------------------------------
            // 🔥 ODA KULLANIM YÜZDESİ (Donut Chart)
            // ----------------------------------------------------------
            ViewBag.RoomUsage = _context.Rooms
                .Select(room => new
                {
                    RoomName = room.Name,
                    Count = _context.Reservations.Count(r => r.RoomId == room.RoomId)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            return View();
        }
    }
}



