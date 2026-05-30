using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using MeOrg.Extensions;
using System.Diagnostics;

namespace MeOrg;

public interface IMediaOrganizer
{
    Task Organize(
        DirectoryInfo source,
        DirectoryInfo target,
        TimeSpan dayOffset,
        bool skipDedupe,
        CancellationToken cancellationToken);
}

public class MediaOrganizer : IMediaOrganizer
{
    private readonly ParallelOptions _parallelOptions;
    private readonly OrganizeRunMetrics _metrics;
    private readonly ISpectreConsole _console;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private readonly IBackgroundFileWriter _writer;
    private readonly IDuplicateFileDetector _duplicateDetector;
    private readonly Dictionary<string, string> _suffixedTargetDirectoryLookup;

    public MediaOrganizer(
        IBackgroundFileWriter writer,
        OrganizeRunMetrics metrics,
        ISpectreConsole console,
        CancellationToken cancellationToken)
    {
        _parallelOptions = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };
        _duplicateDetector = new DuplicateFileDetector();
        _suffixedTargetDirectoryLookup = new();
        _metrics = metrics;
        _console = console;
        _writer = writer;
    }

    public async Task Organize(
        DirectoryInfo source,
        DirectoryInfo target,
        TimeSpan dayOffset,
        bool skipDedupe,
        CancellationToken cancellationToken)
    {
        _metrics.ReportStarted();

        _stopwatch.Start();

        foreach (string subDir in System.IO.Directory.EnumerateDirectories(target.FullName, "*", SearchOption.TopDirectoryOnly))
        {
            string directoryName = Path.GetFileName(subDir);
            if (directoryName.Length >= 10 && DateOnly.TryParseExact(directoryName[..10], "yyyy-MM-dd", out _))
            {
                string prefix = directoryName[..10];
                _suffixedTargetDirectoryLookup.TryAdd(prefix, directoryName);
            }
        }

        _metrics.ReportPreExistingTargetDirLookupCreatedAt(_stopwatch.Elapsed);

        _stopwatch.Restart();

        IEnumerable<string> filesPaths = System.IO.Directory
            .EnumerateFiles(source.FullName, "*", SearchOption.AllDirectories)
            .Where(IsSupportedExtension);

        if (!skipDedupe)
        {
            foreach (string targetPath in System.IO.Directory.EnumerateFiles(target.FullName, "*", SearchOption.AllDirectories))
            {
                if (!IsSupportedExtension(targetPath))
                {
                    continue;
                }

                _duplicateDetector.TrySetFileSeen(targetPath);
            }

            _metrics.ReportPreExistingTargetMediaHashGenerationTime(_stopwatch.Elapsed, _duplicateDetector.SeenCount);

            filesPaths = filesPaths.Where((path) =>
            {
                bool isUnique = _duplicateDetector.TrySetFileSeen(path);
                if (!isUnique)
                {
                    _metrics.ReportDuplicateDetected();
                }

                return isUnique;
            });
        }

        _stopwatch.Restart();

        await Parallel.ForEachAsync(
            filesPaths,
            _parallelOptions,
            (path, ct) => OrganizeFile(path, target, dayOffset, ct));

        _metrics.ReportSourceFileProcessingTime(_stopwatch.Elapsed);

        _stopwatch.Stop();
    }

    private async ValueTask OrganizeFile(
        string path,
        DirectoryInfo target,
        TimeSpan dayOffset,
        CancellationToken cancellationToken)
    {
        string subDirName = Constants.DEFAULT_SUBDIR_NAME;

        if (TryExtractCreationTime(path, out DateTime creationDateTime))
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
        string destinationPath = $"{target.FullName}/{subDirName}/{fileName}";
        await _writer.TryAddFile(fromPath: path, toPath: destinationPath, cancellationToken);
    }

    private bool TryExtractCreationTime(string path, out DateTime createdDateTime)
    {
        try
        {
            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(path);

            var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfdDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out createdDateTime) ?? false)
            {
                return true;
            }

            if (subIfdDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out createdDateTime) ?? false)
            {
                return true;
            }

            if (subIfdDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out createdDateTime) ?? false)
            {
                return true;
            }

            var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
            if (gpsDirectory?.TryGetGpsDate(out createdDateTime) ?? false)
            {
                return true;
            }

            var quickTimeDirectory = directories.OfType<QuickTimeMetadataHeaderDirectory>().FirstOrDefault();
            if (quickTimeDirectory?.TryGetDateTime(QuickTimeMetadataHeaderDirectory.TagCreationDate, out createdDateTime) ?? false)
            {
                return true;
            }

            createdDateTime = File.GetCreationTime(path);
            if (createdDateTime != default)
            {
                _metrics.ReportNonExifCreationDateTime();
                return true;
            }

        }
        catch (ImageProcessingException processingException)
        {
            _console.WriteException(processingException);
        }

        createdDateTime = default;
        return false;
    }

    private static bool IsSupportedExtension(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        if (Constants.SUPPORTED_IMAGE_EXTENSIONS.Contains(ext))
        {
            return true;
        }

        if (Constants.SUPPORTED_VIDEO_EXTENSIONS.Contains(ext))
        {
            return true;
        }

        return false;
    }
}