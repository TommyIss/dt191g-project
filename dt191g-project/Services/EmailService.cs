using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using dt191g_project.Models;

namespace dt191g_project.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendBookingConfirmation(Booking booking, TimeSlot slot, Service service, Company company, string customerEmail)
        {
            var settings = _config.GetSection("EmailSettings");

            var smtp = new SmtpClient
            {
                Host = settings["Host"],
                Port = int.Parse(settings["Port"]),
                EnableSsl = bool.Parse(settings["EnableSSL"]),
                Credentials = new NetworkCredential(settings["UserName"], settings["Password"])
            };

            var mail = new MailMessage
            {
                From = new MailAddress(settings["UserName"], "Bokningssystemet"),
                Subject = "Bekräftelse på din bokning",
                Body = $@"
                    Hej!

                    Din bokning är bekräftad.

                    Företag: {company.Name}
                    Tjänst: {service.Title}
                    Datum: {slot.StartTime:yyyy-MM-dd}
                    Tid: {slot.StartTime:HH:mm} - {slot.EndTime:HH:mm}
                    Pris: {service.Price} kr

                    Tack för att du bokade hos oss!
                    ",
                IsBodyHtml = false
            };

            mail.To.Add(customerEmail);

            await smtp.SendMailAsync(mail);
        }

        public async Task SendCancellationConfirmation(Booking booking, TimeSlot slot, Service service, Company company, string customerEmail)
        {
            var settings = _config.GetSection("EmailSettings");

            var smtp = new SmtpClient
            {
                Host = settings["Host"],
                Port = int.Parse(settings["Port"]),
                EnableSsl = bool.Parse(settings["EnableSSL"]),
                Credentials = new NetworkCredential(settings["UserName"], settings["Password"])
            };

            var mail = new MailMessage
            {
                From = new MailAddress(settings["UserName"], "Bokningssystemet"),
                Subject = "Bekräftelse på avbokning",
                Body = $@"
                    Hej!

                    Din bokning har avbokats.

                    Företag: {company.Name}
                    Tjänst: {service.Title}
                    Datum: {slot.StartTime:yyyy-MM-dd}
                    Tid: {slot.StartTime:HH:mm} - {slot.EndTime:HH:mm}

                    Vi hoppas att vi får se dig igen!
                    ",
                IsBodyHtml = false
            };

            mail.To.Add(customerEmail);

            await smtp.SendMailAsync(mail);
        }

    }
}
