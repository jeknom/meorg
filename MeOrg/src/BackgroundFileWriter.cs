using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace MeOrg;

public interface IBackgroundFileWriter
{
    Task<bool> TryAddFile(string fromPath, string toPath, CancellationToken cancellationToken);
    void Shutdown();
}

public class BackgroundFileWriter : IBackgroundFileWriter
{
    private readonly Channel<(string fromPath, string toPath)> _fileChannel =
        Channel.CreateBounded<(string fromPath, string toPath)>(500);
    private readonly ILogger<BackgroundFileWriter> _logger;

    public BackgroundFileWriter(ILogger<BackgroundFileWriter> logger)
    {
        _logger = logger;
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
                    continue;
                }

                string suffixedName = to;
                do
                {
                    suffixedName = FileNameHelper.GetNextPossiblePath(suffixedName);
                }
                while (File.Exists(suffixedName));

                File.Copy(from, suffixedName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while copying from '{from}' to '{to}'", from, to);
            }
        }
    }

    public async Task<bool> TryAddFile(string fromPath, string toPath, CancellationToken cancellationToken)
    {
        if (!await _fileChannel.Writer.WaitToWriteAsync(cancellationToken))
        {
            _logger.LogError("Failed to write to channel for paths: from '{from}' to '{to}'. The file channel has likely been completed already.", fromPath, toPath);
            return false;
        }

        if (!_fileChannel.Writer.TryWrite((fromPath, toPath)))
        {
            _logger.LogError("Failed to write file from '{from}' to '{to}'.", fromPath, toPath);
            return false;
        }

        return true;
    }

    public void Shutdown()
    {
        _fileChannel.Writer.Complete();
    }
}