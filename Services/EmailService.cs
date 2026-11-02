using MailKit.Net.Smtp;
using MimeKit;

namespace EmployeeManagementSystem.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body)
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _configuration["EmailSettings:SenderName"],
                    _configuration["EmailSettings:SenderEmail"]));

            message.To.Add(
                MailboxAddress.Parse(toEmail));

            message.Subject = subject;

            message.Body = new BodyBuilder
            {
                HtmlBody = body
            }.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _configuration["EmailSettings:SmtpServer"],
                int.Parse(
                    _configuration["EmailSettings:Port"]!),
                MailKit.Security.SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]);

            await smtp.SendAsync(message);

            await smtp.DisconnectAsync(true);
        }
    }
}
