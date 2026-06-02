using MeOrg.Extensions;
using System.Diagnostics;

namespace MeOrg;

public interface IMediaOrganizer
{
    Task Organize(DirectoryInfo source, DirectoryInfo target, TimeSpan dayOffset);
}

public class MediaOrganizer : IMediaOrganizer
{
    private readonly ParallelOptions _parallelOptions;
    private readonly OrganizeRunMetrics _metrics;
    private readonly IConsole _console;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private readonly IBackgroundFileWriter _writer;
    private readonly IDuplicateFileDetector _duplicateDetector;
    private readonly Dictionary<string, string> _suffixedTargetDirectoryLookup;
    private readonly CancellationToken _cancellationToken;

    public MediaOrganizer(
        IBackgroundFileWriter writer,
        IDuplicateFileDetector duplicateDetector,
        OrganizeRunMetrics metrics,
        IConsole console,
        CancellationToken cancellationToken)
    {
        _parallelOptions = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };
        _cancellationToken = cancellationToken;
        _suffixedTargetDirectoryLookup = new();
        _duplicateDetector = duplicateDetector;
        _metrics = metrics;
        _console = console;
        _writer = writer;
    }

    public async Task Organize(
        DirectoryInfo source,
        DirectoryInfo target,
        TimeSpan dayOffset)
    {
        _metrics.ReportStarted();

        Task writerTask = Task.Run(() => _writer.WriteFilesContinuously(_cancellationToken), _cancellationToken);

        _stopwatch.Start();
        _console.WriteInfoLine("Populating pre-existing target directory lookup...");

        List<string> subDirNames = Directory
            .EnumerateDirectories(target.FullName, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList()!;

        var exactDirNames = new HashSet<string>(subDirNames, StringComparer.Ordinal);

        foreach (string dirName in subDirNames)
        {
            if (dirName.Length >= 10 && DateOnly.TryParseExact(dirName[..10], "yyyy-MM-dd", out _))
            {
                string prefix = dirName[..10];
                // If a directory matching the prefix exactly exists, it wins, don't redirect.
                if (!exactDirNames.Contains(prefix))
                {
                    _suffixedTargetDirectoryLookup.TryAdd(prefix, dirName);
                }
            }
        }

        _metrics.ReportPreExistingTargetDirLookupCreatedAt(_stopwatch.Elapsed);

        _stopwatch.Restart();

        IEnumerable<string> targetMediaPaths = Directory
            .EnumerateFiles(target.FullName, "*", SearchOption.AllDirectories)
            .Where(FileHelper.IsSupportedMediaFileExtension);
        _duplicateDetector.MarkPathsAsSeen(targetMediaPaths);

        _metrics.ReportPreExistingTargetMediaHashGenerationTime(_stopwatch.Elapsed, _duplicateDetector.SeenCount);

        _console.WriteInfoLine("Filtering duplicate source media...");

        IEnumerable<string> filesPaths = Directory
            .EnumerateFiles(source.FullName, "*", SearchOption.AllDirectories)
            .Where(FileHelper.IsSupportedMediaFileExtension);
        filesPaths = _duplicateDetector.MarkAndReturnUnseen(filesPaths);

        _stopwatch.Restart();


        if (filesPaths.Any() && !await _console.Confirm($"This operation will copy '{filesPaths.Count()}' files to target directory. Continue?", _cancellationToken))
        {
            _console.WriteInfoLine("Organize canceled, have a nice day!");
            return;
        }

        _console.WriteInfoLine("Copying files...");
        await Parallel.ForEachAsync(
            filesPaths,
            _parallelOptions,
            (path, ct) => OrganizeFile(path, target, dayOffset, ct));

        _metrics.ReportSourceFileProcessingTime(_stopwatch.Elapsed);

        _console.WriteInfoLine("Wrapping up...");
        _stopwatch.Stop();

        _writer.Shutdown();

        await writerTask;

        _console.WriteReport();
    }

    private async ValueTask OrganizeFile(
        string path,
        DirectoryInfo target,
        TimeSpan dayOffset,
        CancellationToken cancellationToken)
    {
        string subDirName = Constants.DEFAULT_SUBDIR_NAME;

        if (FileHelper.TryExtractExifCreationDateTime(path, _console, out DateTime creationDateTime) ||
            FileHelper.TryExtractFileCreationDateTime(path, _console, out creationDateTime))
        {
            DateTime withOffset = creationDateTime - dayOffset;
            if (creationDateTime.Day != withOffset.Day)
            {
                creationDateTime = withOffset;
            }

            subDirName = creationDateTime.ToMeorgDateString();
        }

        if (_suffixedTargetDirectoryLookup.TryGetValue(subDirName, out string? suffixedSubDirName))
        {
            subDirName = suffixedSubDirName;
        }

        string fileName = Path.GetFileName(path);
        string destinationPath = Path.Combine(target.FullName, subDirName, fileName);
        await _writer.TryAddFile(fromPath: path, toPath: destinationPath, cancellationToken);
    }
}