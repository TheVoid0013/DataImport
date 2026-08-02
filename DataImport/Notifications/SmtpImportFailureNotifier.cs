using System.Net;
using System.Net.Mail;
using DataImport.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataImport.Notifications;

public class SmtpImportFailureNotifier : IImportFailureNotifier
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpImportFailureNotifier> _logger;

    public SmtpImportFailureNotifier(IOptions<EmailOptions> opts, ILogger<SmtpImportFailureNotifier> logger)
    {
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task NotifyAsync(string importName, Exception ex, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient(_opts.Smtp.Host, _opts.Smtp.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_opts.Smtp.User, _opts.Smtp.AppPassword)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_opts.Smtp.From),
                Subject = $"[ALERT] Import failed: {importName}",
                Body = $"Import '{importName}' failed at {DateTime.UtcNow:u}.\n\nException:\n{ex}"
            };

            foreach (var recipient in _opts.Smtp.To)
                message.To.Add(recipient);

            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Import failure notification sent for {ImportName}", importName);
        }
        catch (Exception sendEx)
        {
            // Never let a notification failure take down the import process.
            _logger.LogError(sendEx, "Failed to send import-failure notification for {ImportName}", importName);
        }
    }
}