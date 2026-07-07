using System.Diagnostics;

namespace MeOrg;

public class OrganizeRunMetrics
{
    public int CopyCount => _copyCount;
    public int DuplicateCount => _duplicateCount;
    public double ElapsedSeconds => _stopwatch.Elapsed.TotalSeconds;
    public int TotalFileCount { get; private set; }
    public TimeSpan TargetMediaHashGenerationTime { get; private set; }
    public TimeSpan SourceFileProcessingTime { get; private set; }

    private int _copyCount = 0;
    private int _duplicateCount = 0;

    private readonly Stopwatch _stopwatch = new Stopwatch();

    public OrganizeRunMetrics()
    {
        _stopwatch.Start();
    }

    public void ReportTargetMediaHashGenerationTime(TimeSpan elapsed)
    {
        TargetMediaHashGenerationTime = elapsed;
    }

    public void ReportSourceFileProcessingTime(TimeSpan elapsed)
    {
        SourceFileProcessingTime = elapsed;
    }

    public void ReportTotalFileCount(int totalFileCount)
    {
        TotalFileCount = totalFileCount;
    }

    public void ReportFileCopied()
    {
        Interlocked.Increment(ref _copyCount);
    }

    public void ReportDuplicateDetected()
    {
        Interlocked.Increment(ref _duplicateCount);
    }
}