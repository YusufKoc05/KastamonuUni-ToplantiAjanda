using System;
using System.Collections.Generic;
using System.Text;
using KastamonuMeetingRoom.Models;

namespace KastamonuMeetingRoom.Services
{
    public class CalendarIntegrationService
    {
        /// <summary>
        /// Tek bir toplantı için RFC 5545 standartlarında .ics (iCalendar) içeriği üretir.
        /// Outlook, Apple Calendar, Google Calendar ve tüm mobil ajandalarla tam uyumludur.
        /// </summary>
        public string GenerateIcs(Reservation reservation)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//Kastamonu Universitesi//Toplanti ve Ajanda Sistemi//TR");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");
            sb.AppendLine("X-WR-CALNAME:Kastamonu Üniversitesi Toplantı Ajandası");
            sb.AppendLine("X-WR-TIMEZONE:Europe/Istanbul");

            AppendEvent(sb, reservation);

            sb.AppendLine("END:VCALENDAR");
            return sb.ToString();
        }

        /// <summary>
        /// Birden fazla toplantı için toplu .ics takvimi üretir (Kullanıcının veya odanın tüm ajandası).
        /// </summary>
        public string GenerateMultipleIcs(IEnumerable<Reservation> reservations, string calendarName = "Kastamonu Üniversitesi Toplantı Takvimi")
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//Kastamonu Universitesi//Toplanti ve Ajanda Sistemi//TR");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");
            sb.AppendLine($"X-WR-CALNAME:{EscapeText(calendarName)}");
            sb.AppendLine("X-WR-TIMEZONE:Europe/Istanbul");

            foreach (var res in reservations)
            {
                AppendEvent(sb, res);
            }

            sb.AppendLine("END:VCALENDAR");
            return sb.ToString();
        }

        private void AppendEvent(StringBuilder sb, Reservation res)
        {
            var startTimeUtc = res.StartTime.ToUniversalTime();
            var endTimeUtc = res.EndTime.ToUniversalTime();
            var nowUtc = DateTime.UtcNow;

            string roomName = res.Room?.Name ?? "Toplantı Salonu";
            string roomLocation = string.IsNullOrEmpty(res.Room?.Location) ? "" : $" ({res.Room.Location})";
            string location = $"{roomName}{roomLocation}, Kastamonu Üniversitesi";

            string organizerName = res.User?.FullName ?? "Kastamonu Üniversitesi";
            string organizerEmail = res.User?.Email ?? "bilgi@kastamonu.edu.tr";

            string description = string.IsNullOrEmpty(res.Description)
                ? $"Kastamonu Üniversitesi {roomName} toplantısı."
                : $"{res.Description}\n\nToplantı Salonu: {roomName}\nDüzenleyen: {organizerName}";

            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:ku-meeting-{res.ReservationId}-{res.CreatedAt:yyyyMMddTHHmmssZ}@kastamonu.edu.tr");
            sb.AppendLine($"DTSTAMP:{nowUtc:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTSTART:{startTimeUtc:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTEND:{endTimeUtc:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"SUMMARY:{EscapeText(res.Title)}");
            sb.AppendLine($"DESCRIPTION:{EscapeText(description)}");
            sb.AppendLine($"LOCATION:{EscapeText(location)}");
            sb.AppendLine($"STATUS:{(res.IsCancelled ? "CANCELLED" : "CONFIRMED")}");
            sb.AppendLine($"ORGANIZER;CN={EscapeText(organizerName)}:mailto:{organizerEmail}");
            sb.AppendLine("CLASS:PUBLIC");
            sb.AppendLine("END:VEVENT");
        }

        /// <summary>
        /// Doğrudan Google Takvim'de yeni etkinlik oluşturma web URL'i üretir.
        /// </summary>
        public string GetGoogleCalendarUrl(Reservation reservation)
        {
            var startUtc = reservation.StartTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
            var endUtc = reservation.EndTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
            var title = Uri.EscapeDataString(reservation.Title ?? "Kastamonu Üniversitesi Toplantısı");

            string roomName = reservation.Room?.Name ?? "Toplantı Salonu";
            string roomLocation = string.IsNullOrEmpty(reservation.Room?.Location) ? "" : $" ({reservation.Room.Location})";
            var location = Uri.EscapeDataString($"{roomName}{roomLocation}, Kastamonu Üniversitesi");

            string organizerName = reservation.User?.FullName ?? "Yetkili";
            string details = Uri.EscapeDataString(
                $"Kastamonu Üniversitesi Toplantı ve Ajanda Sistemi\n" +
                $"Salon: {roomName}\n" +
                $"Düzenleyen: {organizerName}\n" +
                $"Açıklama / Gündem: {reservation.Description ?? "Belirtilmedi"}"
            );

            return $"https://calendar.google.com/calendar/render?action=TEMPLATE&text={title}&dates={startUtc}/{endUtc}&details={details}&location={location}";
        }

        /// <summary>
        /// Doğrudan Outlook Live / Web Takviminde yeni etkinlik oluşturma web URL'i üretir.
        /// </summary>
        public string GetOutlookCalendarUrl(Reservation reservation)
        {
            var start = reservation.StartTime.ToString("yyyy-MM-ddTHH:mm:ss");
            var end = reservation.EndTime.ToString("yyyy-MM-ddTHH:mm:ss");
            var title = Uri.EscapeDataString(reservation.Title ?? "Kastamonu Üniversitesi Toplantısı");

            string roomName = reservation.Room?.Name ?? "Toplantı Salonu";
            string roomLocation = string.IsNullOrEmpty(reservation.Room?.Location) ? "" : $" ({reservation.Room.Location})";
            var location = Uri.EscapeDataString($"{roomName}{roomLocation}, Kastamonu Üniversitesi");

            string details = Uri.EscapeDataString(
                $"Kastamonu Üniversitesi Toplantı Sistemi\n" +
                $"Salon: {roomName}\n" +
                $"Açıklama: {reservation.Description ?? "Belirtilmedi"}"
            );

            return $"https://outlook.live.com/calendar/0/deeplink/compose?subject={title}&startdt={start}&enddt={end}&body={details}&location={location}";
        }

        private string EscapeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n");
        }
    }
}
