using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KastamonuMeetingRoom.Models
{
    public class Reservation
    {
        public int ReservationId { get; set; }

        // 📌 Oda Id
        [Required(ErrorMessage = "Oda seçimi zorunludur.")]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        // 📌 Kullanıcı Id
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // 📌 Başlık
        [Required(ErrorMessage = "Başlık alanı zorunludur.")]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        // 🏷️ Toplantı Türü (Örn: Akademik Kurul, Tez Savunması, Senato, BAP Proje...)
        [StringLength(50)]
        public string? MeetingType { get; set; } = "Genel Toplantı";

        // 👥 Katılımcı Listesi (E-posta veya İsimler)
        public string? Attendees { get; set; }

        // 📌 Açıklama / Gündem
        public string? Description { get; set; }

        // 📌 Başlangıç – Bitiş saatleri
        [Required(ErrorMessage = "Başlangıç zamanı zorunludur.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Bitiş zamanı zorunludur.")]
        public DateTime EndTime { get; set; }

        // 📌 Oluşturulma tarihi
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsCancelled { get; set; } = false;
    }
}
