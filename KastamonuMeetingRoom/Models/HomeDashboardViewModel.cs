using System.Collections.Generic;

namespace KastamonuMeetingRoom.Models
{
    public class HomeDashboardViewModel
    {
        public int TotalRooms { get; set; }
        public int TodayMeetingsCount { get; set; }
        public int UpcomingMeetingsCount { get; set; }
        public int MyMeetingsCount { get; set; }
        public List<Reservation> TodayAgenda { get; set; } = new List<Reservation>();
        public List<Room> FeaturedRooms { get; set; } = new List<Room>();
    }
}
