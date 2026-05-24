using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using Microsoft.Extensions.Logging;
using MeOrg.Extensions;

namespace MeOrg;

public interface IMediaOrganizer
{
    Task Organize(DirectoryInfo source, DirectoryInfo target, CancellationToken cancellationToken);
}

public class MediaOrganizer : IMediaOrganizer
{
    private readonly ParallelOptions _parallelOptions;
    private readonly ILogger<MediaOrganizer> _logger;
    private readonly IBackgroundFileWriter _writer;
    private readonly IDuplicateFileDetector _duplicateDetector;

    public MediaOrganizer(
        IBackgroundFileWriter writer,
        ILogger<MediaOrganizer> logger,
        CancellationToken cancellationToken)
    {
        _parallelOptions = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };
        _duplicateDetector = new DuplicateFileDetector();
        _logger = logger;
        _writer = writer;
    }

    public async Task Organize(
        DirectoryInfo source,
        DirectoryInfo target,
        CancellationToken cancellationToken)
    {
        foreach (string targetPath in System.IO.Directory.EnumerateFiles(target.FullName, "*", SearchOption.AllDirectories))
        {
            if (!IsSupportedExtension(targetPath))
            {
                continue;
            }

            _duplicateDetector.TrySetFileSeen(targetPath);
        }

        var filesPaths = System.IO.Directory
            .EnumerateFiles(source.FullName, "*", SearchOption.AllDirectories)
            .Where(IsSupportedExtension)
            .Where(_duplicateDetector.TrySetFileSeen);

        await Parallel.ForEachAsync(
            filesPaths,
            _parallelOptions,
            (path, ct) => OrganizeFile(path, target, ct));
    }

    private async ValueTask OrganizeFile(
        string path,
        DirectoryInfo target,
        CancellationToken cancellationToken)
    {
        string subDirName = Constants.DEFAULT_SUBDIR_NAME;

        if (TryExtractCreationTime(path, out DateTime creationDateTime))
        {
            subDirName = creationDateTime.ToMeorgDateString();
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

            _logger.LogWarning("Could not parse creation date for file '{path}'", path);
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