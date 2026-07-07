using Xunit.Abstractions;

namespace MeOrg.Tests;

public class BackgroundFileWriterTests
{
    private readonly TestConsole _console;
    private readonly OrganizeRunMetrics _metrics;
    private readonly BackgroundFileWriter _writer;
    private readonly MockFileAccess _fileAccess;

    public BackgroundFileWriterTests(ITestOutputHelper output)
    {
        _metrics = new OrganizeRunMetrics();
        _console = new TestConsole(output, _metrics);
        _fileAccess = new MockFileAccess();
        _writer = new BackgroundFileWriter(_metrics, _console, _fileAccess);
    }

    [Fact(Timeout = 10000)]
    public async Task Writer_Can_Only_Start_Once()
    {
        Task writerTask = _writer.WriteFilesContinuously(CancellationToken.None);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _writer.WriteFilesContinuously(CancellationToken.None));
        Assert.Equal("Writer has already been started!", exception.Message);
    }

    [Fact(Timeout = 10000)]
    public async Task Writer_Logs_Progress_In_10_Percent_Steps()
    {
        _metrics.TotalFileCount = 1000;
        Task writerTask = _writer.WriteFilesContinuously(CancellationToken.None);
        for (int i = 0; i < 1000; i++)
        {
            Assert.True(await _writer.TryAddFile($"test-from-path-{i}", $"test-to-path-{i}", CancellationToken.None));
        }

        _writer.Shutdown();
        await writerTask;

        List<string> progressLogs = _console.Logs.Where(log => log.EndsWith("files copied...")).ToList();

        Assert.Equal(10, progressLogs.Count);
        Assert.Equal("100/1000 files copied...", progressLogs[0]);
        Assert.Equal("200/1000 files copied...", progressLogs[1]);
        Assert.Equal("300/1000 files copied...", progressLogs[2]);
        Assert.Equal("400/1000 files copied...", progressLogs[3]);
        Assert.Equal("500/1000 files copied...", progressLogs[4]);
        Assert.Equal("600/1000 files copied...", progressLogs[5]);
        Assert.Equal("700/1000 files copied...", progressLogs[6]);
        Assert.Equal("800/1000 files copied...", progressLogs[7]);
        Assert.Equal("900/1000 files copied...", progressLogs[8]);
        Assert.Equal("1000/1000 files copied...", progressLogs[9]);
    }
}