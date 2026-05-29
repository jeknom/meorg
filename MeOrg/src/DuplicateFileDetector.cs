using MeOrg.Extensions;

namespace MeOrg;

interface IDuplicateFileDetector
{
    bool TrySetFileSeen(string path);
    int SeenCount { get; }
}

public class DuplicateFileDetector : IDuplicateFileDetector
{
    private readonly HashSet<string> _seen = new();

    public int SeenCount => _seen.Count;

    public bool TrySetFileSeen(string path)
    {
        using FileStream fileStream = File.Open(path, FileMode.Open);
        string hash = fileStream.GenerateSampledHash();

        if (_seen.Contains(hash))
        {
            return false;
        }

        _seen.Add(hash);

        return true;
    }
}