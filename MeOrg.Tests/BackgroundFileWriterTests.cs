using MeOrg.Exceptions;
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
        _writer = new BackgroundFileWriter(
            _metrics,
            _console,
            _fileAccess,
            options: new BackgroundFileWriterOptions
            {
                RetryBackoffDelayCoefficient = 0
            });
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
        _metrics.ReportTotalFileCount(1000);
        Task writerTask = _writer.WriteFilesContinuously(CancellationToken.None);
        for (int i = 0; i < 1000; i++)
        {
            Assert.True(await _writer.TryAddFile($"test-from-path-{i}", $"test-to-path-{i}", CancellationToken.None));
        }

        _writer.Shutdown();
        await writerTask;

        List<string> progressLogs = _console.Logs.Where(log => log.EndsWith("files copied...")).ToList();

        Assert.Equal(10, progressLogs.Count);
        Assert.Equal("100/1000 files copied...", progressLogs[9]);
        Assert.Equal("200/1000 files copied...", progressLogs[8]);
        Assert.Equal("300/1000 files copied...", progressLogs[7]);
        Assert.Equal("400/1000 files copied...", progressLogs[6]);
        Assert.Equal("500/1000 files copied...", progressLogs[5]);
        Assert.Equal("600/1000 files copied...", progressLogs[4]);
        Assert.Equal("700/1000 files copied...", progressLogs[3]);
        Assert.Equal("800/1000 files copied...", progressLogs[2]);
        Assert.Equal("900/1000 files copied...", progressLogs[1]);
        Assert.Equal("1000/1000 files copied...", progressLogs[0]);
    }

    [Fact(Timeout = 10000)]
    public async Task Writer_Skips_Progress_Logging_When_Less_Than_200_Files()
    {
        _metrics.ReportTotalFileCount(199);
        Task writerTask = _writer.WriteFilesContinuously(CancellationToken.None);
        for (int i = 0; i < 199; i++)
        {
            Assert.True(await _writer.TryAddFile($"test-from-path-{i}", $"test-to-path-{i}", CancellationToken.None));
        }

        _writer.Shutdown();
        await writerTask;

        List<string> progressLogs = _console.Logs.Where(log => log.EndsWith("files copied...")).ToList();
        Assert.Empty(progressLogs);
    }

    [Fact(Timeout = 20000)]
    public async Task Writer_Retries_After_IO_Exception()
    {
        _metrics.ReportTotalFileCount(1);
        Task writerTask = _writer.WriteFilesContinuously(CancellationToken.None);
        _fileAccess.QueueIOExceptionOnCopy(amount: 2);
        await _writer.TryAddFile($"test-from", "test-to", CancellationToken.None);
        _writer.Shutdown();
        await writerTask;

        List<string> logs = _console.Logs.ToList();
        Assert.StartsWith("System.IO.IOException: I/O error occurred.", logs[4]);
        Assert.EndsWith("Previous copy attempt failed, retrying with backoff (attempt 1/3)", logs[3]);
        Assert.StartsWith("System.IO.IOException: I/O error occurred.", logs[2]);
        Assert.EndsWith("Previous copy attempt failed, retrying with backoff (attempt 2/3)", logs[1]);
        Assert.EndsWith("Copy succeeded after '2' retry attempts.", logs[0]);
        Assert.Equal(0, _writer.FailedCopies);
    }

    [Fact(Timeout = 10000)]
    public async Task Writer_Does_Not_Retry_On_Generic_Exception()
    {
        _metrics.ReportTotalFileCount(1);
        Task writerTask = _writer.WriteFilesContinuously(CancellationToken.None);
        _fileAccess.QueueGenericExceptionOnCopy(amount: 1);
        await _writer.TryAddFile($"test-from", "test-to", CancellationToken.None);
        _writer.Shutdown();
        await writerTask;

        List<string> logs = _console.Logs.ToList();
        Assert.Equal(2, logs.Count);
        Assert.StartsWith("Non-retryable exception encountered. Failed copy allowance status: 1/", logs[0]);
        Assert.StartsWith("System.Exception: Exception of type 'System.Exception' was thrown.", logs[1]);
        Assert.Equal(1, _writer.FailedCopies);
    }

    [Fact(Timeout = 20000)]
    public async Task Writer_Throws_Exit_Exception_After_Too_Many_Failed_Copies()
    {
        _metrics.ReportTotalFileCount(5);
        Task writerTask = _writer.WriteFilesContinuously(CancellationToken.None);
        _fileAccess.QueueGenericExceptionOnCopy(amount: 4);
        for (int i = 0; i < 4; i++)
        {
            await _writer.TryAddFile($"test-from-{i}", $"test-to-{i}", CancellationToken.None);
        }
        _fileAccess.QueueIOExceptionOnCopy(amount: 4);
        await _writer.TryAddFile($"test-from-with-io-ex", $"test-to-with-io-ex", CancellationToken.None);
        ErrorExitException ex = await Assert.ThrowsAsync<ErrorExitException>(async () => await writerTask);
        Assert.Equal(ExitCode.TooManyFailedCopies, ex.Code);
        Assert.Equal("Exiting due to high amount of failed copies. Failed copy amount: '5'", ex.Message);
        Assert.Equal(5, _writer.FailedCopies);
    }
}
