using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using KastamonuMeetingRoom.Data;
using KastamonuMeetingRoom.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KastamonuMeetingRoom.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var todayReservations = _context.Reservations
                .Include(r => r.Room)
                .Include(r => r.User)
                .Where(r => !r.IsCancelled && r.StartTime.Date == today)
                .OrderBy(r => r.StartTime)
                .ToList();

            var featuredRooms = _context.Rooms
                .Take(3)
                .ToList();

            // Odaların anlık canlı doluluk durumunu hesapla
            foreach (var room in featuredRooms)
            {
                var ongoing = todayReservations
                    .FirstOrDefault(r => r.RoomId == room.RoomId && now >= r.StartTime && now <= r.EndTime);

                if (ongoing != null)
                {
                    room.IsCurrentlyOccupied = true;
                    room.CurrentMeetingTitle = ongoing.Title;
                    room.RemainingMinutes = (int)Math.Max(0, (ongoing.EndTime - now).TotalMinutes);
                }
                else
                {
                    room.IsCurrentlyOccupied = false;
                    room.NextMeeting = todayReservations
                        .Where(r => r.RoomId == room.RoomId && r.StartTime > now)
                        .OrderBy(r => r.StartTime)
                        .FirstOrDefault();
                }
            }

            var model = new HomeDashboardViewModel
            {
                TotalRooms = _context.Rooms.Count(),
                TodayMeetingsCount = todayReservations.Count,
                UpcomingMeetingsCount = _context.Reservations.Count(r => !r.IsCancelled && r.StartTime >= now),
                MyMeetingsCount = !string.IsNullOrEmpty(userId)
                    ? _context.Reservations.Count(r => !r.IsCancelled && r.UserId == userId && r.EndTime >= now)
                    : 0,
                TodayAgenda = todayReservations.Take(6).ToList(),
                FeaturedRooms = featuredRooms
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
