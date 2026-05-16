using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MeOrg.Tests;

public class OrganizeTests : IDisposable
{
    private readonly IMediaOrganizer _mediaOrganizer;
    private readonly BackgroundFileWriter _writer;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();


    public OrganizeTests()
    {
        _writer = new BackgroundFileWriter(NullLogger<BackgroundFileWriter>.Instance);
        _mediaOrganizer = new MediaOrganizer(_writer, NullLogger<MediaOrganizer>.Instance, _cts.Token);
        Task.Run(() => _writer.WriteFilesContiniously(_cts.Token));
    }

    public void Dispose()
    {
        _writer.Shutdown();
        _cts.Cancel();
    }

    [Fact(Timeout = 10000)]
    public async Task Organize_Media_To_Target_Directory()
    {
        string targetDirPath = Path.Combine(AppContext.BaseDirectory, "TestTarget", Guid.NewGuid().ToString());

        DirectoryInfo source = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "TestAssets"));
        DirectoryInfo target = Directory.CreateDirectory(targetDirPath);
        await _mediaOrganizer.Organize(source, target, CancellationToken.None);
        await AssertFileExists(Path.Combine(targetDirPath, "2026-04-25/IMG_4146.HEIC"), timeoutMs: 1000);
        await AssertFileExists(Path.Combine(targetDirPath, "2026-04-26/IMG_4158.MOV"), timeoutMs: 1000);
        await AssertFileExists(Path.Combine(targetDirPath, "Misc/butterfly.webp"), timeoutMs: 1000);
        await AssertFileExists(Path.Combine(targetDirPath, "Misc/giraffe.jpg"), timeoutMs: 1000);
        await AssertFileExists(Path.Combine(targetDirPath, "Misc/jumble.png"), timeoutMs: 1000);
    }

    private async Task AssertFileExists(string filePath, int timeoutMs)
    {
        CancellationTokenSource cts = new();
        cts.CancelAfter(timeoutMs);

        while (!cts.Token.IsCancellationRequested)
        {
            if (File.Exists(filePath))
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Fail($"Asserting file '{filePath}' exists timed out.");
    }
}
