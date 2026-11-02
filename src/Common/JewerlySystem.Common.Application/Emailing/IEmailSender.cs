namespace JewerlySystem.Common.Application.Emailing;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent);
}
