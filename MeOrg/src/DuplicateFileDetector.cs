using System.Collections.Concurrent;
using MeOrg.Extensions;

namespace MeOrg;

public interface IDuplicateFileDetector
{
    int SeenCount { get; }
    void MarkPathsAsSeen(IEnumerable<string> paths);
    List<string> MarkAndReturnUnseen(IEnumerable<string> paths);
}

public class DuplicateFileDetector : IDuplicateFileDetector
{
    private readonly ConcurrentDictionary<string, byte> _seen = new();
    private readonly OrganizeRunMetrics _metrics;
    private readonly IConsole _console;

    public int SeenCount => _seen.Count;

    public DuplicateFileDetector(OrganizeRunMetrics metrics, IConsole console)
    {
        _metrics = metrics;
        _console = console;
    }

    public void MarkPathsAsSeen(IEnumerable<string> paths)
    {
        int count = 0;
        foreach (string path in paths)
        {
            TrySetFileSeen(path);
            count++;
        }

        _console.WriteInfoLine($"Marked '{count}' files as seen.");
    }

    public List<string> MarkAndReturnUnseen(IEnumerable<string> paths)
    {
        var unseen = paths.Where((p) =>
        {
            if (TrySetFileSeen(p))
            {
                return true;
            }

            _metrics.ReportDuplicateDetected();

            return false;
        }).ToList();

        _console.WriteInfoLine($"Found '{unseen.Count}' unseen files.");

        return unseen;
    }

    private bool TrySetFileSeen(string path)
    {
        using FileStream fileStream = File.OpenRead(path);
        string hash = fileStream.GenerateSampledHash();

        return _seen.TryAdd(hash, 0);
    }
}