using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace MeetingRoomProject.Services
{
    public class FakeEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Gerçek e-posta göndermiyoruz.
            // Sadece sistem hata vermesin diye boş bırakıyoruz.
            return Task.CompletedTask;
        }
    }
}
