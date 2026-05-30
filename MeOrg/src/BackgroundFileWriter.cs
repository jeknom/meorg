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
    private readonly OrganizeRunMetrics _report;
    private readonly ISpectreConsole _console;

    public BackgroundFileWriter(OrganizeRunMetrics report, ISpectreConsole console)
    {
        _report = report;
        _console = console;
    }

    public async Task WriteFilesContinuously(CancellationToken cancellationToken)
    {
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

                    _report.ReportFileCopied();

                    continue;
                }

                string suffixedName = to;
                do
                {
                    suffixedName = FileNameHelper.GetNextPossiblePath(suffixedName);
                }
                while (File.Exists(suffixedName));

                File.Copy(from, suffixedName);

                _report.ReportFileCopied();
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