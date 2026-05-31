using System.Collections.Concurrent;
using MeOrg.Extensions;

namespace MeOrg;

interface IDuplicateFileDetector
{
    bool TrySetFileSeen(string path);
    int SeenCount { get; }
}

public class DuplicateFileDetector : IDuplicateFileDetector
{
    private readonly ConcurrentDictionary<string, byte> _seen = new();

    public int SeenCount => _seen.Count;

    public bool TrySetFileSeen(string path)
    {
        using FileStream fileStream = File.Open(path, FileMode.Open);
        string hash = fileStream.GenerateSampledHash();

        return _seen.TryAdd(hash, 0);
    }
}