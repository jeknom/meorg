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
    private bool isStarted;

    public BackgroundFileWriter(OrganizeRunMetrics metrics, IConsole console)
    {
        _metrics = metrics;
        _console = console;
    }

    public async Task WriteFilesContinuously(CancellationToken cancellationToken)
    {
        if (isStarted)
        {
            throw new InvalidOperationException("Writer has already been started!");
        }

        isStarted = true;

        await foreach (var (from, to) in _fileChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                string? directory = Path.GetDirectoryName(to);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(to))
                {
                    File.Copy(from, to);

                    _metrics.ReportFileCopied();

                    continue;
                }

                string suffixedName = to;
                do
                {
                    suffixedName = FileHelper.GetFilepathWithIncrementedNumericalSuffix(suffixedName);
                }
                while (File.Exists(suffixedName));

                File.Copy(from, suffixedName);

                _metrics.ReportFileCopied();
            }
            catch (Exception ex)
            {
                _console.WriteException(ex);
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