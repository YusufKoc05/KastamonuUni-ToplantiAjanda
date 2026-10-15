using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace KastamonuMeetingRoom.Hubs
{
    public class CalendarHub : Hub
    {
        /// <summary>
        /// Tüm bağlı kullanıcılara takvimde bir değişiklik (ekleme, saat güncelleme, iptal) olduğunu anında bildirir.
        /// </summary>
        public async Task NotifyCalendarChanged(string action, int reservationId, string title)
        {
            await Clients.All.SendAsync("ReceiveCalendarUpdate", action, reservationId, title);
        }
    }
}
