using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MeetingRoomProject.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpClient = new SmtpClient(_config["SMTP:Host"])
            {
                Port = int.Parse(_config["SMTP:Port"]),
                Credentials = new NetworkCredential(_config["SMTP:User"], _config["SMTP:Pass"]),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_config["SMTP:User"], "Kastamonu Meeting Room"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mail.To.Add(email);

            return smtpClient.SendMailAsync(mail);
        }
    }
}
