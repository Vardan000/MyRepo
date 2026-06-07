using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;


namespace LoginAppWebApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendVerificationCodeAsync(string toEmail, string code)
        {
            return SendEmailAsync(toEmail, "Email Verification", $"Your verification code is: {code}");
        }

        public Task SendPasswordResetCodeAsync(string toEmail, string code)
        {
            return SendEmailAsync(toEmail, "Password Reset", $"Your password reset code is: {code}");
        }

        private async Task SendEmailAsync(string toEmail, string subject, string text)
        {
            string fromEmail = _configuration["Smtp:FromEmail"]!;
            string username = _configuration["Smtp:Username"]!;
            string password = _configuration["Smtp:Password"]!;

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress("LoginApp", fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = text
            };

            using SmtpClient client = new SmtpClient();

            await client.ConnectAsync("smtp-relay.brevo.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}

