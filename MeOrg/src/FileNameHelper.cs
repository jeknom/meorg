using System.Text.RegularExpressions;

namespace MeOrg;

public static class FileNameHelper
{
    private static readonly string SUFFIX_REGEX = @"\((\d+)\)$";

    public static string GetNextPossiblePath(string currentPath)
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
}