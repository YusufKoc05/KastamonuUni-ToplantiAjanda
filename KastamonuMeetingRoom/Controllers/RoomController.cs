using KastamonuMeetingRoom.Data;
using KastamonuMeetingRoom.Models;

using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace MeetingRoomProject.Controllers
{
    public class RoomController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var rooms = _context.Rooms.ToList();
            return View(rooms);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
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

        public IActionResult Edit(int id)
        {
            var room = _context.Rooms.Find(id);
            return View(room);
        }

        [HttpPost]
        public IActionResult Edit(Room room)
        {
            _context.Rooms.Update(room);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var room = _context.Rooms.Find(id);
            _context.Rooms.Remove(room);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
