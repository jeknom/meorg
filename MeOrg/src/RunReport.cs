using Microsoft.Extensions.Logging;

namespace MeOrg;

public class RunReport
{
    private readonly ILogger<RunReport> _logger;
    private int _duplicateCount = 0;
    private int _unreliableCreationDateCount = 0;
    private int _copyCount = 0;

    public RunReport(ILogger<RunReport> logger)
    {
        _logger = logger;
    }

    public void ReportFileCopied()
    {
        _copyCount++;
    }

    public void ReportDuplicateDetected()
    {
        _duplicateCount++;
    }

    public void ReportUnreliableCreationDate()
    {
        _unreliableCreationDateCount++;
    }

    public void LogReport()
    {
        using (_logger.BeginScope("Organize report:"))
        {
            _logger.LogInformation("Organized files: '{copyCount}'", _copyCount);
            _logger.LogInformation("Detected duplicates: '{duplicateCount}'", _duplicateCount);
            _logger.LogInformation("Unrealiable creation date (non-exif): '{undeterminedCount}'", _unreliableCreationDateCount);
        }
    }
}