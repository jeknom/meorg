using MeOrg.Extensions;
using Xunit.Abstractions;

namespace MeOrg.Tests;

public class DuplicateFileDetectorTests
{
    private readonly IConsole _console;
    private readonly DuplicateFileDetector _detector;
    private readonly OrganizeRunMetrics _metrics;
    private readonly IFileAccess _fileAccess;

    public DuplicateFileDetectorTests(ITestOutputHelper output)
    {
        _console = new TestConsole(output);
        _metrics = new OrganizeRunMetrics();
        _fileAccess = new MockFileAccess();
        _detector = new DuplicateFileDetector(_metrics, _fileAccess, _console);
    }

    [Fact]
    public void Only_Reports_Unseen()
    {
        _detector.MarkPathsAsSeen(["A"], CancellationToken.None);
        _detector.MarkAndReturnUnseen(["A", "B"], CancellationToken.None);

        Assert.Equal(1, _metrics.DuplicateCount);
    }

    [Fact]
    public void Mark_Paths_As_Seen_Throws_If_Cancelled()
    {
        CancellationTokenSource cts = new CancellationTokenSource(millisecondsDelay: 0);

        Assert.Throws<OperationCanceledException>(() => _detector.MarkPathsAsSeen(["A"], cts.Token));
    }

    [Fact]
    public void Mark_And_Return_Unseen_Throws_If_Cancelled()
    {
        CancellationTokenSource cts = new CancellationTokenSource(millisecondsDelay: 0);

        Assert.Throws<OperationCanceledException>(() => _detector.MarkAndReturnUnseen(["A"], cts.Token));
    }
}
