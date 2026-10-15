using System;
using System.Linq;
using KastamonuMeetingRoom.Data;
using KastamonuMeetingRoom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KastamonuMeetingRoom.Controllers
{
    public class RoomController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 📌 Herkes salonları inceleyebilir (Canlı Müsaitlik Hesaplamalı)
        [AllowAnonymous]
        public IActionResult Index(string? department, bool? videoConference)
        {
            var now = DateTime.Now;
            var roomsQuery = _context.Rooms.AsQueryable();

            if (!string.IsNullOrEmpty(department))
            {
                roomsQuery = roomsQuery.Where(r => r.Department == department);
            }

            if (videoConference == true)
            {
                roomsQuery = roomsQuery.Where(r => r.HasVideoConference);
            }

            var rooms = roomsQuery.OrderBy(r => r.Name).ToList();

            // Bugünün aktif rezervasyonlarını çekip canlı durumlarını hesaplayalım
            var todayReservations = _context.Reservations
                .Include(r => r.User)
                .Where(r => !r.IsCancelled && r.StartTime.Date == now.Date)
                .ToList();

            foreach (var room in rooms)
            {
                var currentMeeting = todayReservations
                    .FirstOrDefault(r => r.RoomId == room.RoomId && now >= r.StartTime && now <= r.EndTime);

                if (currentMeeting != null)
                {
                    room.IsCurrentlyOccupied = true;
                    room.CurrentMeetingTitle = currentMeeting.Title;
                    room.CurrentMeetingUser = currentMeeting.User?.FullName ?? "Akademisyen";
                    room.RemainingMinutes = (int)Math.Max(0, (currentMeeting.EndTime - now).TotalMinutes);
                }
                else
                {
                    room.IsCurrentlyOccupied = false;
                    // Sıradaki toplantıyı bul
                    room.NextMeeting = todayReservations
                        .Where(r => r.RoomId == room.RoomId && r.StartTime > now)
                        .OrderBy(r => r.StartTime)
                        .FirstOrDefault();
                }
            }

            ViewBag.SelectedDepartment = department;
            ViewBag.SelectedVideoConf = videoConference ?? false;
            ViewBag.Departments = _context.Rooms
                .Where(r => !string.IsNullOrEmpty(r.Department))
                .Select(r => r.Department!)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return View(rooms);
        }

        // 📌 📺 SALON KAPI EKRANI / TABLET KİOSK MODU (/Room/DoorDisplay/1)
        [AllowAnonymous]
        public IActionResult DoorDisplay(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room == null)
                return NotFound();

            var now = DateTime.Now;
            var todayReservations = _context.Reservations
                .Include(r => r.User)
                .Where(r => r.RoomId == id && !r.IsCancelled && r.StartTime.Date == now.Date)
                .OrderBy(r => r.StartTime)
                .ToList();

            var currentMeeting = todayReservations
                .FirstOrDefault(r => now >= r.StartTime && now <= r.EndTime);

            if (currentMeeting != null)
            {
                room.IsCurrentlyOccupied = true;
                room.CurrentMeetingTitle = currentMeeting.Title;
                room.CurrentMeetingUser = currentMeeting.User?.FullName ?? "Akademisyen";
                room.RemainingMinutes = (int)Math.Max(0, (currentMeeting.EndTime - now).TotalMinutes);
            }
            else
            {
                room.IsCurrentlyOccupied = false;
                room.NextMeeting = todayReservations
                    .Where(r => r.StartTime > now)
                    .OrderBy(r => r.StartTime)
                    .FirstOrDefault();
            }

            ViewBag.TodayReservations = todayReservations;
            return View(room);
        }

        // 📌 Sadece Admin salon ekleyebilir
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Room());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
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

        // 📌 Sadece Admin salon düzenleyebilir
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room == null)
                return NotFound();
            return View(room);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
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

        // 📌 Sadece Admin salon silebilir
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room == null)
                return NotFound();

            var relatedReservations = _context.Reservations.Where(r => r.RoomId == id).ToList();
            if (relatedReservations.Any())
            {
                _context.Reservations.RemoveRange(relatedReservations);
            }

            _context.Rooms.Remove(room);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
