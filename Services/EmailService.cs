using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace LoginWeb.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Device Monitor App", _config["EmailSettings:FromEmail"]));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = message };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_config["EmailSettings:SmtpServer"],
                                   int.Parse(_config["EmailSettings:SmtpPort"]),
                                   SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["EmailSettings:SmtpUsername"],
                                        _config["EmailSettings:SmtpPassword"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
