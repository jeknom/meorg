using System.Diagnostics;
using MeOrg.Extensions;
using Xunit.Abstractions;

namespace MeOrg.Tests;

public class OrganizeTests : IDisposable
{
    private readonly IMediaOrganizer _mediaOrganizer;
    private readonly IBackgroundFileWriter _writer;
    private readonly OrganizeRunMetrics _metrics;
    private readonly IConsole _console;
    private readonly IDuplicateFileDetector _duplicateDetector;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly string _sourceBase = new(Path.Combine(AppContext.BaseDirectory, "TestFiles/Scenarios"));
    private readonly string _targetPath = Path.Combine(AppContext.BaseDirectory, "TestTarget", Guid.NewGuid().ToString());
    private readonly DirectoryInfo _target;
    private readonly Stopwatch _stopwatch = new Stopwatch();

    public OrganizeTests(ITestOutputHelper output)
    {
        _target = Directory.CreateDirectory(_targetPath);
        _stopwatch.Start();
        _metrics = new OrganizeRunMetrics();
        _console = new TestConsole(output, _metrics);
        _writer = new BackgroundFileWriter(_metrics, _console);
        _duplicateDetector = new DuplicateFileDetector(_metrics, _console);
        _mediaOrganizer = new MediaOrganizer(_writer, _duplicateDetector, _metrics, _console, _cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _stopwatch.Stop();

        Directory.Delete(_targetPath, recursive: true);
    }

    [Fact]
    public async Task Organize_Supported_Files()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "SupportedFiles"));
        await _mediaOrganizer.Organize(source, _target, dayOffset: TimeSpan.Zero);
        await AssertFileExists(GetTargetPath("2026-04-25/IMG_4146.HEIC"));
        await AssertFileExists(GetTargetPath("2026-04-26/IMG_4158.MOV"));
        await AssertFileExists(GetTargetPath("2026-04-26/butterfly.webp"));
        await AssertFileExists(GetTargetPath("2026-04-26/giraffe.jpg"));
        await AssertFileExists(GetTargetPath("2026-04-26/jumble.png"));
    }

    [Fact]
    public async Task Organize_Does_Not_Copy_Unsupported_Files()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "UnsupportedFiles"));
        await _mediaOrganizer.Organize(source, _target, dayOffset: TimeSpan.Zero);
        await AssertFileDoesNotExistAfterDelay(GetTargetPath("Misc/some.txt"));
    }

    [Fact(Timeout = 10000)]
    public async Task Organize_Namesakes()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "Namesakes"));
        await _mediaOrganizer.Organize(source, _target, dayOffset: TimeSpan.Zero);
        await AssertFileExists(GetTargetPath("2026-05-24/IMG_NAMESAKE.HEIC"));
        await AssertFileExists(GetTargetPath("2026-05-24/IMG_NAMESAKE (2).HEIC"));
    }

    [Fact(Timeout = 10000)]
    public async Task Organize_Duplicates()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "Duplicates"));
        await _mediaOrganizer.Organize(source, _target, dayOffset: TimeSpan.Zero);
        await AssertFileExists(GetTargetPath("2026-05-24/IMG_DUPLICATE_A.HEIC"));
        await AssertFileDoesNotExistAfterDelay(GetTargetPath("2026-05-24/IMG_DUPLICATE_B.HEIC"));
    }

    [Fact(Timeout = 10000)]
    public async Task Organize_Supports_Directory_Suffix_In_Existing_Target()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "TargetExistsWithSuffix"));
        Directory.CreateDirectory(GetTargetPath("2026-05-17 test suffix"));
        await _mediaOrganizer.Organize(source, _target, dayOffset: TimeSpan.Zero);
        await AssertFileExists(GetTargetPath("2026-05-17 test suffix/giraffe.jpg"));
    }

    [Fact(Timeout = 10000)]
    public async Task Organize_Respects_Day_Offset()
    {
        DirectoryInfo source = new(Path.Combine(_sourceBase, "RespectDayOffset"));
        Assert.True(FileHelper.TryExtractExifCreationDateTime(Path.Combine(source.FullName, "TAKEN_AT_12_22.HEIC"), _console, out DateTime testFileCreatedAt));
        Assert.Equal("2026-05-24", testFileCreatedAt.ToMeorgDateString());

        await _mediaOrganizer.Organize(source, _target, dayOffset: TimeSpan.FromHours(13));
        await AssertFileExists(GetTargetPath("2026-05-23/TAKEN_AT_12_22.HEIC"));
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

    private string GetTargetPath(string relativePath)
    {
        string path = Path.Combine(_target.FullName, relativePath);
        return path;
    }
}
