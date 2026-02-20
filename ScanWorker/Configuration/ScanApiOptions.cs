namespace ScanWorker.Configuration;

public class ScanApiOptions
{
    public const string SectionName = "ScanApi";

    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

