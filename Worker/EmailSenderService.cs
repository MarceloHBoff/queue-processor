using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Worker;

public class EmailSenderService
{
    private readonly IConfiguration _configuration;
    private readonly SmtpClient _smtpClient;
    private readonly string _fromAddress = "";
    private readonly string _fromName = "";

    public EmailSenderService(IConfiguration configuration)
    {
        _configuration = configuration;
        _fromAddress = _configuration.GetValue("EmailUser", "");
        _fromName = _configuration.GetValue("EmailName", "");
        _smtpClient = new SmtpClient(_configuration.GetValue("EmailHost", ""), Convert.ToInt32(_configuration.GetValue("EmailPort", "")))
        {
            UseDefaultCredentials = false,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(_configuration.GetValue("EmailUser", ""), _configuration.GetValue("EmailPassword", ""))
        };
    }

    public async Task SendEmail(string toAddress, string body, string subject)
    {
        var from = new MailAddress(_fromAddress, _fromName, System.Text.Encoding.UTF8);
        var to = new MailAddress(toAddress);

        using var message = new MailMessage(from, to)
        {
            SubjectEncoding = System.Text.Encoding.UTF8,
            Subject = subject,

            BodyEncoding = System.Text.Encoding.UTF8,
            Body = body,
        };

        try
        {
            await _smtpClient.SendMailAsync(message);
        }
        catch { }
    }
}
