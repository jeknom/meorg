using Xunit.Abstractions;

namespace MeOrg.Tests;

public class OrganizeTests : IDisposable
{
    private readonly IMediaOrganizer _mediaOrganizer;
    private readonly BackgroundFileWriter _writer;
    private readonly RunReport _report;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly string _sourceBase = new(Path.Combine(AppContext.BaseDirectory, "TestFiles/Scenarios"));
    private readonly DirectoryInfo _target = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "TestTarget", Guid.NewGuid().ToString()));

    public OrganizeTests(ITestOutputHelper output)
    {
        _report = new RunReport(new XUnitLogger<RunReport>(output));
        _writer = new BackgroundFileWriter(_report, new XUnitLogger<BackgroundFileWriter>(output));
        _mediaOrganizer = new MediaOrganizer(_writer, new XUnitLogger<MediaOrganizer>(output), _report, _cts.Token);
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
        await _mediaOrganizer.Organize(source, _target, skipDedupe: false, CancellationToken.None);
        await AssertFileExists(GetTargetFilePath("2026-04-25/IMG_4146.HEIC"));
        await AssertFileExists(GetTargetFilePath("2026-04-26/IMG_4158.MOV"));
        await AssertFileExists(GetTargetFilePath("Misc/butterfly.webp"));
        await AssertFileExists(GetTargetFilePath("Misc/giraffe.jpg"));
        await AssertFileExists(GetTargetFilePath("Misc/jumble.png"));
    }

    [Fact]
    public async Task Organize_Does_Not_Copy_Unsupported_Files()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "UnsupportedFiles"));
        await _mediaOrganizer.Organize(source, _target, skipDedupe: false, CancellationToken.None);
        await AssertFileDoesNotExistAfterDelay(GetTargetFilePath("Misc/some.txt"));
    }

    [Fact(Timeout = 10000)]
    public async Task Organize_Namesakes()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "Namesakes"));
        await _mediaOrganizer.Organize(source, _target, skipDedupe: false, CancellationToken.None);
        await AssertFileExists(GetTargetFilePath("2026-05-24/IMG_NAMESAKE.HEIC"));
        await AssertFileExists(GetTargetFilePath("2026-05-24/IMG_NAMESAKE (2).HEIC"));
    }

    [Fact(Timeout = 10000)]
    public async Task Organize_Duplicates()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "Duplicates"));
        await _mediaOrganizer.Organize(source, _target, skipDedupe: false, CancellationToken.None);
        await AssertFileExists(GetTargetFilePath("2026-05-24/IMG_DUPLICATE_A.HEIC"));
        await AssertFileDoesNotExistAfterDelay(GetTargetFilePath("2026-05-24/IMG_DUPLICATE_B.HEIC"));
    }

    private static async Task AssertFileExists(string filePath, int timeoutMs = 1000)
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

    private static async Task AssertFileDoesNotExistAfterDelay(string filePath, int delay = 1000)
    {
        CancellationTokenSource cts = new();
        cts.CancelAfter(delay);

        while (!cts.Token.IsCancellationRequested)
        {
            Assert.False(File.Exists(filePath));
            await Task.Delay(100);
        }
    }

    private string GetTargetFilePath(string relativePath)
    {
        string path = Path.Combine(_target.FullName, relativePath);
        return path;
    }
}
