using System.Collections.Immutable;

namespace MeOrg;

public static class Constants
{
    public static readonly HashSet<string> SUPPORTED_IMAGE_EXTENSIONS = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".jpe",
        ".jfif",
        ".png",
        ".gif",
        ".bmp",
        ".tif",
        ".tiff",
        ".webp",
        ".heic",
        ".heif",
        ".avif",
        ".jxl",
        ".raw",
        ".arw",
        ".cr2",
        ".cr3",
        ".nef",
        ".nrw",
        ".orf",
        ".raf",
        ".rw2",
        ".dng",
        ".pef",
        ".srw",
        ".x3f",
        ".svg",
        ".ico",
        ".psd",
    };

    public static readonly HashSet<string> SUPPORTED_VIDEO_EXTENSIONS = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".m4v",
        ".mov",
        ".qt",
        ".avi",
        ".mkv",
        ".webm",
        ".wmv",
        ".flv",
        ".mpg",
        ".mpeg",
        ".mpe",
        ".m2v",
        ".mts",
        ".m2ts",
        ".ts",
        ".3gp",
        ".3g2",
        ".ogv",
        ".ogg",
        ".vob",
        ".rm",
        ".rmvb",
        ".asf",
        ".f4v",
        ".mxf",
        ".insv",
    };

    public const string DEFAULT_SUBDIR_NAME = "Misc";

    public static readonly HashSet<DateOnly> KNOWN_DEFAULT_DATES = new()
    {
        DateOnly.FromDateTime(DateTime.UnixEpoch),
        new DateOnly(year: 1970, month: 1, day: 1),
        new DateOnly(year: 1904, month: 1, day: 1), // MetadataExtractor converts a default EXIF value to this in some cases
        new DateOnly(year: 2106, month: 2, day: 7), // On Mac, I noticed the unix epoch creation date sometimes wraps around to this, I guess due to it being stored as an unsigned 32-bit integer
    };
}
