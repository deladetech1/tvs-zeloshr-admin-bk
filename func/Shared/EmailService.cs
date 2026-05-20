namespace ZelosHR.Functions.Shared;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public class EmailService : IEmailService
{
    public Task<bool> SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        // TODO: integrate SMTP / SendGrid
        return Task.FromResult(true);
    }
}
