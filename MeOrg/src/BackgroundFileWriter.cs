using System.Threading.Channels;

namespace MeOrg;

public interface IBackgroundFileWriter
{
    Task WriteFilesContinuously(CancellationToken cancellationToken);
    Task<bool> TryAddFile(string fromPath, string toPath, CancellationToken cancellationToken);
    void Shutdown();
}

public class BackgroundFileWriter : IBackgroundFileWriter
{
    private readonly Channel<(string fromPath, string toPath)> _fileChannel =
        Channel.CreateBounded<(string fromPath, string toPath)>(500);
    private readonly OrganizeRunMetrics _metrics;
    private readonly IConsole _console;
    private readonly IFileAccess _fileAccess;
    private const int MIN_FILES_FOR_PROGRESS_REPORT = 200;
    private bool _isStarted;
    private bool _shouldReportProgress;

    public BackgroundFileWriter(OrganizeRunMetrics metrics, IConsole console, IFileAccess fileAccess)
    {
        _metrics = metrics;
        _console = console;
        _fileAccess = fileAccess;
    }

    public async Task WriteFilesContinuously(CancellationToken cancellationToken)
    {
        if (_isStarted)
        {
            throw new InvalidOperationException("Writer has already been started!");
        }

        _isStarted = true;

        _shouldReportProgress = _metrics.TotalFileCount >= MIN_FILES_FOR_PROGRESS_REPORT;

        await foreach (var (from, to) in _fileChannel.Reader.ReadAllAsync(cancellationToken))
        {
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

                    continue;
                }

                string suffixedName = to;
                do
                {
                    suffixedName = FileHelper.GetFilepathWithIncrementedNumericalSuffix(suffixedName);
                }
                while (_fileAccess.FileExists(suffixedName));

                _fileAccess.CopyFile(from, suffixedName);

                _metrics.ReportFileCopied();
            }
            catch (Exception ex)
            {
                _console.WriteException(ex);
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