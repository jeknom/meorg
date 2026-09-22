namespace MeOrg;

public class NoOpDuplicateFileDetector : IDuplicateFileDetector
{
    public int SeenCount => 0;

    public NoOpDuplicateFileDetector()
    {
    }

    public void MarkPathsAsSeen(IEnumerable<string> paths, CancellationToken ct)
    {
    }

    public List<string> MarkAndReturnUnseen(IEnumerable<string> paths, CancellationToken ct)
    {
        return paths.ToList();
    }
}
