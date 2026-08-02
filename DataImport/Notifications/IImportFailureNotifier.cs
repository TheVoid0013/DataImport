namespace DataImport.Notifications;

public interface IImportFailureNotifier
{
    Task NotifyAsync(string importName, Exception ex, CancellationToken ct = default);
}