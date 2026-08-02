namespace DataImport.Configuration;

public class EmailOptions
{
    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string From { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
    public List<string> To { get; set; } = new();
}