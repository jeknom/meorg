using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<MediaOrganizer> _logger;
    private readonly RunReport _report;
    private readonly Stopwatch _stopwatch;
    private readonly IBackgroundFileWriter _writer;
    private readonly IDuplicateFileDetector _duplicateDetector;
    private readonly Dictionary<string, string> _suffixedTargetDirectoryLookup;

    public MediaOrganizer(
        IBackgroundFileWriter writer,
        ILogger<MediaOrganizer> logger,
        RunReport report,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        _parallelOptions = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };
        _duplicateDetector = new DuplicateFileDetector();
        _suffixedTargetDirectoryLookup = new();
        _logger = logger;
        _report = report;
        _stopwatch = stopwatch;
        _writer = writer;
    }

    public async Task Organize(
        DirectoryInfo source,
        DirectoryInfo target,
        TimeSpan dayOffset,
        bool skipDedupe,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start organizing files. From '{source}' to '{target}'. Dedupe enabled: '{skipDedupe}'. Day hours offset: '{dayOffset}'.", source.FullName, target.FullName, !skipDedupe, dayOffset.Hours);

        foreach (string subDir in System.IO.Directory.EnumerateDirectories(target.FullName, "*", SearchOption.TopDirectoryOnly))
        {
            string directoryName = Path.GetFileName(subDir);
            if (directoryName.Length >= 10 && DateOnly.TryParseExact(directoryName[..10], "yyyy-MM-dd", out _))
            {
                string prefix = directoryName[..10];
                _suffixedTargetDirectoryLookup.TryAdd(prefix, directoryName);
            }
        }

        _logger.LogInformation("Lookup for existing target directories has been created. There are '{existingCount}' existing directories. (Elapsed seconds: {secs})", _suffixedTargetDirectoryLookup.Count, _stopwatch.Elapsed.TotalSeconds);

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

            _logger.LogInformation("Hash generation for existing target media completed. There are {targetMediaCount} media files in the target directory. (Elapsed seconds: {secs})", _duplicateDetector.SeenCount, _stopwatch.Elapsed.TotalSeconds);

            filesPaths = filesPaths.Where((path) =>
            {
                bool isUnique = _duplicateDetector.TrySetFileSeen(path);
                if (!isUnique)
                {
                    _report.ReportDuplicateDetected();
                }

                return isUnique;
            });
        }

        await Parallel.ForEachAsync(
            filesPaths,
            _parallelOptions,
            (path, ct) => OrganizeFile(path, target, dayOffset, ct));

        _logger.LogInformation("All files have been handed over to background writer. (Elapsed seconds: {secs})", _stopwatch.Elapsed.TotalSeconds);
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
                _report.ReportUnreliableCreationDate();
                return true;
            }

        }
        catch (ImageProcessingException processingException)
        {
            _logger.LogWarning(processingException, "Failed to process image path '{path}'", path);
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