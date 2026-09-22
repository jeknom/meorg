using System.Collections.Concurrent;

namespace MeOrg;

public interface IDuplicateFileDetector
{
    int SeenCount { get; }
    void MarkPathsAsSeen(IEnumerable<string> paths, CancellationToken ct);
    List<string> MarkAndReturnUnseen(IEnumerable<string> paths, CancellationToken ct);
}

public class DuplicateFileDetector : IDuplicateFileDetector
{
    private readonly ConcurrentDictionary<string, byte> _seen = new();
    private readonly OrganizeRunMetrics _metrics;
    private readonly IFileAccess _fileAccess;
    private readonly IConsole _console;

    public int SeenCount => _seen.Count;

    public DuplicateFileDetector(
        OrganizeRunMetrics metrics,
        IFileAccess fileAccess,
        IConsole console)
    {
        _metrics = metrics;
        _fileAccess = fileAccess;
        _console = console;
    }

    public void MarkPathsAsSeen(IEnumerable<string> paths, CancellationToken ct)
    {
        int count = 0;
        foreach (string path in paths)
        {
            ct.ThrowIfCancellationRequested();
            TrySetFileSeen(path);
            count++;
        }

        _console.WriteInfoLine($"Marked '{count}' files as seen.");
    }

    public List<string> MarkAndReturnUnseen(IEnumerable<string> paths, CancellationToken ct)
    {
        List<string> result = new();
        foreach (var path in paths)
        {
            ct.ThrowIfCancellationRequested();

            if (TrySetFileSeen(path))
            {
                result.Add(path);
            }
            else
            {
                _metrics.ReportDuplicateDetected();
            }

        }

        _console.WriteInfoLine($"Found '{result.Count}' unseen files.");

        return result;
    }

    private bool TrySetFileSeen(string path)
    {
        string hash = _fileAccess.GenerateSampledHashFromFile(path);

        return _seen.TryAdd(hash, 0);
    }
}
