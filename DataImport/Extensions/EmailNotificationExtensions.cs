using DataImport.Configuration;
using DataImport.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DataImport.Extensions;

public static class EmailNotificationExtensions
{
    public static HostApplicationBuilder AddEmailNotifications(this HostApplicationBuilder builder)
    {
        builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
        builder.Services.AddSingleton<IImportFailureNotifier, SmtpImportFailureNotifier>();
        return builder;
    }
}