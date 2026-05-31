using System.Diagnostics;

namespace MeOrg;

public class OrganizeRunMetrics
{
    public int CopyCount => _copyCount;
    public int DuplicateCount => _duplicateCount;
    public double ElapsedSeconds => _stopwatch.Elapsed.TotalSeconds;
    public int PreExistingMediaInTargetCount { get; private set; }
    public DateTime StartTime { get; private set; }
    public TimeSpan PreExistingTargetDirLookupCreationTime { get; private set; }
    public TimeSpan PreExistingTargetMediaHashGenerationTime { get; private set; }
    public TimeSpan SourceFileProcessingTime { get; private set; }

    private int _copyCount = 0;
    private int _duplicateCount = 0;

    private readonly Stopwatch _stopwatch = new Stopwatch();

    public OrganizeRunMetrics()
    {
        _stopwatch.Start();
    }

    public void ReportStarted()
    {
        if (StartTime != default)
        {
            return;
        }

        StartTime = DateTime.UtcNow;
    }

    public void ReportPreExistingTargetDirLookupCreatedAt(TimeSpan elapsed)
    {
        if (PreExistingTargetDirLookupCreationTime != default)
        {
            return;
        }

        PreExistingTargetDirLookupCreationTime = elapsed;
    }

    public void ReportPreExistingTargetMediaHashGenerationTime(TimeSpan elapsed, int existingMediaFileCount)
    {
        if (PreExistingTargetMediaHashGenerationTime != default)
        {
            return;
        }

        PreExistingTargetMediaHashGenerationTime = elapsed;
        PreExistingMediaInTargetCount = existingMediaFileCount;
    }

    public void ReportSourceFileProcessingTime(TimeSpan elapsed)
    {
        if (SourceFileProcessingTime != default)
        {
            return;
        }

        SourceFileProcessingTime = elapsed;
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