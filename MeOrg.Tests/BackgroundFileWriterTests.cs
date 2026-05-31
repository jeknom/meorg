using Xunit.Abstractions;

namespace MeOrg.Tests;

public class BackgroundFileWriterTests
{
    private readonly IConsole _console;
    private readonly OrganizeRunMetrics _metrics;
    private readonly BackgroundFileWriter _writer;

    public BackgroundFileWriterTests(ITestOutputHelper output)
    {
        _metrics = new OrganizeRunMetrics();
        _console = new TestConsole(output, _metrics);
        _writer = new BackgroundFileWriter(_metrics, _console);
    }

    [Fact(Timeout = 10000)]
    public async Task Writer_Can_Only_Start_Once()
    {
        Task writerTask = _writer.WriteFilesContinuously(CancellationToken.None);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _writer.WriteFilesContinuously(CancellationToken.None));
        Assert.Equal("Writer has already been started!", exception.Message);
    }
}