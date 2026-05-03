using System.CommandLine;
using System.Globalization;
using MeOrg.Validators;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using MetadataExtractor.Formats.Icc;
using MetadataExtractor.Formats.QuickTime;
using Microsoft.Extensions.Logging;

namespace MeOrg.Commands;

public class OrganizeCommand : Command
{
    private readonly IBackgroundFileWriter _writer;
    private readonly ILogger _logger;

    public OrganizeCommand(IBackgroundFileWriter writer, ILogger logger) : base(
        "organize",
        "Used to organize media from an unorganized source directory into target directory.")
    {
        _writer = writer;
        _logger = logger;

        Option<DirectoryInfo> sourceDirOption = new("--source")
        {
            Description = "Unorganized media source directory.",
            Required = true
        };

        sourceDirOption.Validators.Add(result => result.IsDirectoryPathWithReadPermissions());
        Options.Add(sourceDirOption);

        Option<DirectoryInfo> targetDirOption = new("--target")
        {
            Description = "Directory where to copy your organized media.",
            Required = true
        };
        targetDirOption.Validators.Add(result => result.IsDirectoryPathWithWritePermissions());
        Options.Add(targetDirOption);


        SetAction((parseResult, ct) => OrganizeAction(
            sourceDir: parseResult.GetValue(sourceDirOption)!,
            targetDir: parseResult.GetValue(targetDirOption)!,
            cancellationToken: ct));
    }

    private async Task OrganizeAction(DirectoryInfo sourceDir, DirectoryInfo targetDir, CancellationToken cancellationToken)
    {
        var filesPaths = System.IO.Directory
            .EnumerateFiles(sourceDir.FullName, "*", SearchOption.AllDirectories)
            .Where(IsSupportedExtension);

        foreach (var path in filesPaths)
        {
            CultureInfo enUs = new CultureInfo("en-US");
            DateTime date = default;
            try
            {
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(path);

                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (subIfdDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out date) ?? false)
                {
                    continue;
                }

                if (subIfdDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out date) ?? false)
                {
                    continue;
                }

                if (subIfdDirectory?.TryGetDateTime(ExifDirectoryBase.TagDateTime, out date) ?? false)
                {
                    continue;
                }

                var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
                if (gpsDirectory?.TryGetGpsDate(out date) ?? false)
                {
                    continue;
                }

                var quickTimeDirectory = directories.OfType<QuickTimeMetadataHeaderDirectory>().FirstOrDefault();
                if (quickTimeDirectory?.TryGetDateTime(QuickTimeMetadataHeaderDirectory.TagCreationDate, out date) ?? false)
                {
                    continue;
                }

                _logger.LogWarning("Could not parse creation date for file '{path}'", path);
            }
            catch (ImageProcessingException processingException)
            {
                _logger.LogWarning(processingException, "Failed to process image path '{path}'", path);
            }
            finally
            {
                if (date != default)
                {
                    _logger.LogInformation("Date: {date}", date.ToString());
                }
            }
        }
    }
    // 2. When encountering any of the specified files of type, enqueue them to bounded channel or concurrent queue (what is the difference?)

    // 3. Queue could live in its own class and have a continious task that writes files
    // 4. Profit?

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