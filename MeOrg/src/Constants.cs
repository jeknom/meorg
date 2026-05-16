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
}
