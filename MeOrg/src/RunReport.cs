using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MeOrg;

public class RunReport
{
    private readonly ILogger<RunReport> _logger;
    private int _duplicateCount = 0;
    private int _unreliableCreationDateCount = 0;
    private int _copyCount = 0;
    private readonly Stopwatch _stopwatch;

    public RunReport(Stopwatch stopwatch, ILogger<RunReport> logger)
    {
        _stopwatch = stopwatch;
        _logger = logger;
    }

    public void ReportFileCopied()
    {
        Interlocked.Increment(ref _copyCount);
    }

    public void ReportDuplicateDetected()
    {
        Interlocked.Increment(ref _duplicateCount);
    }

    public void ReportUnreliableCreationDate()
    {
        Interlocked.Increment(ref _unreliableCreationDateCount);
    }

    public void LogReport()
    {
        using (_logger.BeginScope("Organize report:"))
        {
            _logger.LogInformation("Organized files:\t\t'{copyCount}'\nDetected duplicates:\t'{duplicateCount}'\nNon-exif creation date:\t'{undeterminedCount}'\nSeconds elapsed:\t\t'{secs}'", _copyCount, _duplicateCount, _unreliableCreationDateCount, _stopwatch.Elapsed.TotalSeconds);
        }
    }
}