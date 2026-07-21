using System.Text.RegularExpressions;
using MeOrg.Extensions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;

namespace MeOrg;

public static partial class FileHelper
{
    [GeneratedRegex(@"\((\d+)\)$")]
    private static partial Regex SuffixRegex();

    public static string GetFilepathWithIncrementedNumericalSuffix(string currentPath)
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentPath);
        Match match = SuffixRegex().Match(fileNameWithoutExtension);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int result))
        {
            fileNameWithoutExtension = SuffixRegex().Replace(fileNameWithoutExtension, $"({result + 1})");
        }
        else
        {
            fileNameWithoutExtension = $"{fileNameWithoutExtension} (2)";
        }
        string dir = Path.GetDirectoryName(currentPath) ?? string.Empty;
        return Path.Combine(dir, $"{fileNameWithoutExtension}{Path.GetExtension(currentPath)}");
    }

    public static bool TryExtractMediaMetadataCreationDateTime(string path, IConsole console, out DateTime createdDateTime)
    {
        bool isVideo = Constants.SUPPORTED_VIDEO_EXTENSIONS.Contains(Path.GetExtension(path));

        try
        {
            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(path);

            if (isVideo)
            {
                var qtHeaderDirectory = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
                if (qtHeaderDirectory != null && qtHeaderDirectory.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out createdDateTime))
                {
                    createdDateTime = createdDateTime.SpecifyUtcAndConvertToLocal();

                    return true;
                }

                var qtTrackHeaderDirectory = directories.OfType<QuickTimeTrackHeaderDirectory>().FirstOrDefault();
                if (qtTrackHeaderDirectory != null && qtTrackHeaderDirectory.TryGetDateTime(QuickTimeTrackHeaderDirectory.TagCreated, out createdDateTime))
                {
                    createdDateTime = createdDateTime.SpecifyUtcAndConvertToLocal();

                    return true;
                }
            }
            else
            {
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out createdDateTime))
                {
                    return true;
                }

                if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out createdDateTime))
                {
                    return true;
                }

                var subIfd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                if (subIfd0Directory != null && subIfd0Directory.TryGetDateTime(ExifIfd0Directory.TagDateTime, out createdDateTime))
                {
                    return true;
                }
            }
        }
        catch (ImageProcessingException processingException)
        {
            console.WriteException(processingException);
        }
        catch (IOException ioException)
        {
            console.WriteException(ioException);
        }

        createdDateTime = default;

        return false;
    }

    public static bool TryExtractFileSystemGuesstimatedOriginalDateTime(string path, IConsole console, out DateTime guesstimate)
    {
        bool hasCreationTime = TryExtractFileCreationDateTime(path, console, out DateTime createTime);
        bool hasModifiedTime = TryExtractFileModifiedDateTime(path, console, out DateTime modifyTime);

        if (!hasModifiedTime && hasCreationTime)
        {
            guesstimate = createTime;
        }
        else if (!hasCreationTime && hasModifiedTime)
        {
            guesstimate = modifyTime;
        }
        else if (hasCreationTime && hasModifiedTime)
        {
            guesstimate = modifyTime < createTime ? modifyTime : createTime;
        }
        else
        {
            guesstimate = default;
            return false;
        }

        return true;
    }

    public static bool IsDateSuspectedDefaultValue(DateTime dateTime)
    {
        return dateTime == default || Constants.KNOWN_DEFAULT_DATES.Contains(DateOnly.FromDateTime(dateTime));
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

    public static bool TryExtractFileModifiedDateTime(string path, IConsole console, out DateTime createdDateTime)
    {
        try
        {
            createdDateTime = File.GetLastWriteTime(path);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            console.WriteErrorLine($"Not authorized to read last modified time for '{path}'");
        }

        createdDateTime = default;

        return false;
    }

    public static bool IsSupportedMediaFileExtension(string path)
    {
        string ext = Path.GetExtension(path);
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