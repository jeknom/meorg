using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;

namespace MeOrg;

public static class FileHelper
{
    private static readonly string SUFFIX_REGEX = @"\((\d+)\)$";

    public static string GetFilepathWithIncrementedNumericalSuffix(string currentPath)
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentPath);
        Match match = Regex.Match(fileNameWithoutExtension, SUFFIX_REGEX);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int result))
        {
            fileNameWithoutExtension = Regex.Replace(fileNameWithoutExtension, SUFFIX_REGEX, $"({result + 1})");
        }
        else
        {
            fileNameWithoutExtension = $"{fileNameWithoutExtension} (2)";
        }
        string dir = Path.GetDirectoryName(currentPath) ?? string.Empty;
        return Path.Combine(dir, $"{fileNameWithoutExtension}{Path.GetExtension(currentPath)}");
    }

    public static bool TryExtractExifCreationDateTime(string path, IConsole console, out DateTime createdDateTime)
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

        }
        catch (ImageProcessingException processingException)
        {
            console.WriteException(processingException);
        }

        createdDateTime = default;

        return false;
    }

    public static bool TryExtractFileCreationDateTime(string path, IConsole console, out DateTime createdDateTime)
    {
        try
        {
            createdDateTime = File.GetCreationTime(path);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            console.WriteErrorLine($"Not authorized to read creation time for '{path}'");
        }

        createdDateTime = default;

        return false;
    }

    public static bool IsSupportedMediaFileExtension(string path)
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