using System.ComponentModel.DataAnnotations.Schema;

namespace KastamonuMeetingRoom.Models
{
    public class Room
    {
        public int RoomId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Location { get; set; } = string.Empty;

        // 🏛️ Fakülte / Birim (Örn: Rektörlük, Mühendislik Fakültesi, İİBF...)
        public string? Department { get; set; } = "Genel";

        // 🛠️ Donanım Özellikleri
        public bool HasProjector { get; set; } = true;
        public bool HasVideoConference { get; set; } = false;
        public bool HasSmartBoard { get; set; } = true;
        public bool HasSoundSystem { get; set; } = true;
        public bool HasDisabledAccess { get; set; } = true;
        public bool HasCatering { get; set; } = false;

        // 🟢/🔴 Canlı Müsaitlik Durumu (Veritabanında saklanmaz, anlık hesaplanır)
        [NotMapped]
        public bool IsCurrentlyOccupied { get; set; } = false;

        [NotMapped]
        public string? CurrentMeetingTitle { get; set; }

        [NotMapped]
        public string? CurrentMeetingUser { get; set; }

        [NotMapped]
        public int RemainingMinutes { get; set; } = 0;

        [NotMapped]
        public Reservation? NextMeeting { get; set; }
    }
}
