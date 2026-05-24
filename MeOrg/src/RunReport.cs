using Microsoft.Extensions.Logging;

namespace MeOrg;

public class RunReport
{
    private readonly ILogger<RunReport> _logger;
    private int _duplicateCount = 0;
    private int _undeterminedCreationDate = 0;
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

    public void ReportUndeterminedCreationDate()
    {
        _undeterminedCreationDate++;
    }

    public void LogReport()
    {
        using (_logger.BeginScope("Organize report:"))
        {
            _logger.LogInformation("Organized '{copyCount}' media files into target directory", _copyCount);
            _logger.LogInformation("Detected '{duplicateCount}' duplicates.", _duplicateCount);
            _logger.LogInformation("Failed to determine creation time for '{undeterminedCount}' media files", _undeterminedCreationDate);
        }
    }
}