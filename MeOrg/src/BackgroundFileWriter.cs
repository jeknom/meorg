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

    public async Task WriteFilesContiniously(CancellationToken cancellationToken)
    {
        await foreach (var (from, to) in _fileChannel.Reader.ReadAllAsync(cancellationToken))
        {
            string? directory = Path.GetDirectoryName(to);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(from, to);
        }
    }

    public async Task<bool> TryAddFile(string fromPath, string toPath, CancellationToken cancellationToken)
    {
        if (!await _fileChannel.Writer.WaitToWriteAsync(cancellationToken))
        {
            _logger.LogError("Failed to write file from '{from}' to '{to}'. The file channel has likely been completed already.", fromPath, toPath);
            return false;
        }

        return _fileChannel.Writer.TryWrite((fromPath, toPath));
    }

    public void Shutdown()
    {
        _fileChannel.Writer.Complete();
    }
}