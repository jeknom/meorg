using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace MeOrg.Tests;

public class OrganizeTests : IDisposable
{
    private readonly IMediaOrganizer _mediaOrganizer;
    private readonly BackgroundFileWriter _writer;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly string _sourceBase = new(Path.Combine(AppContext.BaseDirectory, "Scenarios"));
    private readonly DirectoryInfo _target = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "TestTarget", Guid.NewGuid().ToString()));

    public OrganizeTests(ITestOutputHelper output)
    {
        _writer = new BackgroundFileWriter(new XUnitLogger<BackgroundFileWriter>(output));
        _mediaOrganizer = new MediaOrganizer(_writer, new XUnitLogger<MediaOrganizer>(output), _cts.Token);
        Task.Run(() => _writer.WriteFilesContinuously(_cts.Token));
    }

    public void Dispose()
    {
        _writer.Shutdown();
        _cts.Cancel();
    }

    [Fact]
    public async Task Organize_Supported_Files()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "SupportedFiles"));
        await _mediaOrganizer.Organize(source, _target, CancellationToken.None);
        await AssertFileExists(GetTargetFilePath("2026-04-25/IMG_4146.HEIC"));
        await AssertFileExists(GetTargetFilePath("2026-04-26/IMG_4158.MOV"));
        await AssertFileExists(GetTargetFilePath("Misc/butterfly.webp"));
        await AssertFileExists(GetTargetFilePath("Misc/giraffe.jpg"));
        await AssertFileExists(GetTargetFilePath("Misc/jumble.png"));
    }

    [Fact(Timeout = 10000)]
    public async Task Organize_Namesakes()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "Namesakes"));
        await _mediaOrganizer.Organize(source, _target, CancellationToken.None);
        await AssertFileExists(GetTargetFilePath("2026-04-25/IMG_4146.HEIC"));
        await AssertFileExists(GetTargetFilePath("2026-04-25/IMG_4146 (2).HEIC"));
        await AssertFileExists(GetTargetFilePath("2026-04-26/IMG_4158.MOV"));
        await AssertFileExists(GetTargetFilePath("2026-04-26/IMG_4158 (2).MOV"));
        await AssertFileExists(GetTargetFilePath("Misc/giraffe.jpg"));
        await AssertFileExists(GetTargetFilePath("Misc/giraffe (2).jpg"));
    }

    private static async Task AssertFileExists(string filePath, int timeoutMs = 999999999)
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

    private string GetTargetFilePath(string relativePath)
    {
        string path = Path.Combine(_target.FullName, relativePath);
        return path;
    }
}
