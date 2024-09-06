using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using Authentication.Models;
using Microsoft.EntityFrameworkCore;
using Authentication.Context;
using Authentication.Controllers;


namespace Authentication.Services
{
    public struct EmailData() 
    {
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }
    public class EmailService
    {
        public const string SENDER_EMAIL = "xxxx";

        private readonly DataContext _context;
        private readonly ILogger<LoginController> _logger;

        public EmailService(DataContext context, ILogger<LoginController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendEmail(EmailData emailData) 
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress("Authentication App", SENDER_EMAIL));
            email.To.Add(new MailboxAddress("Receiver Name", emailData.Email));

            email.Subject = emailData.Subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Plain)
            {
                Text = emailData.Message
            };

            using (var smtp = new SmtpClient())
            {
                smtp.Connect("smtp.mailersend.net", 587, false);

                smtp.Authenticate("xxxxx", "xxxx");
                smtp.Send(email);
                smtp.Disconnect(true);
            }

            await StoreEmailToDB(emailData);
        }

        private async Task StoreEmailToDB(EmailData emailData) 
        {

            var emailHistory = new EmailHistory
            {
                EmailId = 0,
                MailFrom = SENDER_EMAIL,
                MailTo = emailData.Email,
                Subject = emailData.Subject,
                Body = emailData.Message,
                SentDate = DateTime.Now,
            };

            _logger.Log(LogLevel.Warning, emailData.Email);
            _logger.Log(LogLevel.Warning, emailData.Subject);

            _context.EmailHistory.Add(emailHistory);
            await _context.SaveChangesAsync();
        }
    }
}
