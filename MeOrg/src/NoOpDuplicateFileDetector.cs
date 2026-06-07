namespace MeOrg;

public class NoOpDuplicateFileDetector : IDuplicateFileDetector
{
    public int SeenCount => 0;

    public NoOpDuplicateFileDetector()
    {
    }

    public void MarkPathsAsSeen(IEnumerable<string> paths)
    {
    }

    public List<string> MarkAndReturnUnseen(IEnumerable<string> paths)
    {
        return paths.ToList();
    }
}