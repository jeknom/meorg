using System.Threading.Channels;
using MeOrg.Exceptions;

namespace MeOrg;

public interface IBackgroundFileWriter
{
    Task WriteFilesContinuously(CancellationToken cancellationToken);
    Task<bool> TryAddFile(string fromPath, string toPath, CancellationToken cancellationToken);
    void Shutdown();
}

public class BackgroundFileWriterOptions
{
    public int CopyRetryAttempts { get; init; } = 3;
    public int MaxFailedCopies { get; init; } = 5;
    public int MinFilesForProgressReport { get; init; } = 200;
    public int RetryBackoffDelayCoefficient = 1000;
}

public class BackgroundFileWriter : IBackgroundFileWriter
{
    private readonly Channel<(string fromPath, string toPath)> _fileChannel =
        Channel.CreateBounded<(string fromPath, string toPath)>(500);
    private readonly OrganizeRunMetrics _metrics;
    private readonly IConsole _console;
    private readonly IFileAccess _fileAccess;
    private readonly BackgroundFileWriterOptions _options;
    private bool _isStarted;
    private bool _shouldReportProgress;
    public int FailedCopies { get; private set; }

    public BackgroundFileWriter(
        OrganizeRunMetrics metrics,
        IConsole console,
        IFileAccess fileAccess,
        BackgroundFileWriterOptions options)
    {
        _metrics = metrics;
        _console = console;
        _fileAccess = fileAccess;
        _options = options;
    }

    public async Task WriteFilesContinuously(CancellationToken cancellationToken)
    {
        if (_isStarted)
        {
            throw new InvalidOperationException("Writer has already been started!");
        }

        _isStarted = true;

        _shouldReportProgress = _metrics.TotalFileCount >= _options.MinFilesForProgressReport;

        await foreach (var (from, to) in _fileChannel.Reader.ReadAllAsync(cancellationToken))
        {
            int retryAttempt = 0;

            while (retryAttempt <= _options.CopyRetryAttempts)
            {
                if (retryAttempt > 0)
                {
                    _console.WriteInfoLine($"Previous copy attempt failed for '{from}', retrying with backoff (attempt {retryAttempt}/{_options.CopyRetryAttempts})");
                    await Task.Delay(_options.RetryBackoffDelayCoefficient * (int)Math.Pow(retryAttempt, 2), cancellationToken); // Retry 1 = 1sec, Retry 2 = 4sec, Retry 3 = 9sec
                }

                try
                {
                    string? directory = Path.GetDirectoryName(to);
                    if (directory != null && !_fileAccess.DirectoryExists(directory))
                    {
                        _fileAccess.CreateDirectory(directory);
                    }

                    if (!_fileAccess.FileExists(to))
                    {
                        _fileAccess.CopyFile(from, to);

                        _metrics.ReportFileCopied();

                        if (retryAttempt > 0)
                        {
                            _console.WriteInfoLine($"Copy succeeded after '{retryAttempt}' retry attempts.");
                        }

                        break;
                    }

                    string suffixedName = to;
                    do
                    {
                        suffixedName = FileHelper.GetFilepathWithIncrementedNumericalSuffix(suffixedName);
                    }
                    while (_fileAccess.FileExists(suffixedName));

                    _fileAccess.CopyFile(from, suffixedName);

                    _metrics.ReportFileCopied();

                    if (retryAttempt > 0)
                    {
                        _console.WriteInfoLine($"Copy succeeded after '{retryAttempt}' retry attempts.");
                    }

                    break;
                }
                catch (IOException ioException)
                {
                    _console.WriteException(ioException);
                    retryAttempt++;
                }
                catch (Exception ex)
                {
                    _console.WriteException(ex);
                    FailedCopies++;
                    _console.WriteErrorLine($"Non-retryable exception encountered. Failed copy allowance status: {FailedCopies}/{_options.MaxFailedCopies}.");
                    break;
                }
                finally
                {
                    int currentStep = _metrics.CopyCount * 10 / _metrics.TotalFileCount;
                    int previousStep = (_metrics.CopyCount - 1) * 10 / _metrics.TotalFileCount;

                    if (_shouldReportProgress && currentStep > previousStep)
                    {
                        _console.WriteInfoLine($"{_metrics.CopyCount}/{_metrics.TotalFileCount} files copied...");
                    }
                }
            }

            if (retryAttempt > _options.CopyRetryAttempts)
            {
                FailedCopies++;
                _console.WriteErrorLine($"Retries failed after '{retryAttempt}' attempts. Failed copy allowance status: {FailedCopies}/{_options.MaxFailedCopies}.");
            }

            if (FailedCopies >= _options.MaxFailedCopies)
            {
                throw new ErrorExitException(ExitCode.TooManyFailedCopies, $"Exiting due to high amount of failed copies. Failed copy amount: '{FailedCopies}'");
            }
        }
    }

    public async Task<bool> TryAddFile(string fromPath, string toPath, CancellationToken cancellationToken)
    {
        try
        {
            await _fileChannel.Writer.WriteAsync((fromPath, toPath), cancellationToken);
            return true;
        }
        catch (ChannelClosedException)
        {
            _console.WriteErrorLine($"Failed to write to channel for paths: from '{fromPath}' to '{toPath}'. The file channel has likely been completed already.");
            return false;
        }
    }

    public void Shutdown()
    {
        _fileChannel.Writer.Complete();
    }
}
