using System.Threading.Channels;

namespace MeOrg;

public interface IBackgroundFileWriter
{
    bool TryAddFile(string fromPath, string toPath);
    void Shutdown();
}

public class BackgroundFileWriter : IBackgroundFileWriter
{
    private readonly Channel<(string fromPath, string toPath)> _fileChannel =
        Channel.CreateUnbounded<(string fromPath, string toPath)>();

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

    public bool TryAddFile(string fromPath, string toPath)
    {
        return _fileChannel.Writer.TryWrite((fromPath, toPath));
    }

    public void Shutdown()
    {
        _fileChannel.Writer.Complete();
    }
}