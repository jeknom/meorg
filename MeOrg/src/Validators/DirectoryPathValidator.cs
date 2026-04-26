namespace MeOrg.Validators;

using System.CommandLine.Parsing;

public static partial class OptionParseResultExtensions
{
    public static void NotEmptyString(this OptionResult result)
    {
        if (result.Tokens.Count == 0)
        {
            result.AddError("Source directory path not specified");
        }
    }

    public static void IsDirectory(this OptionResult result)
    {
        string dirPath = result.Tokens.Single().Value;
        if (!Directory.Exists(dirPath))
        {
            result.AddError($"Source directory '{dirPath}' does not exist");
        }
    }

    public static void HasReadPermission(this OptionResult result)
    {
        string path = result.Tokens.Single().Value;

        try
        {
            _ = Directory.EnumerateFileSystemEntries(path).GetEnumerator().MoveNext();
        }
        catch (UnauthorizedAccessException)
        {
            result.AddError($"No read permission for directory '{path}'");
        }
    }

    public static void HasWritePermission(this OptionResult result)
    {
        string path = result.Tokens.Single().Value;
        string probe = Path.Combine(path, $".meorg-write-test-{Guid.NewGuid():N}");
        try
        {
            using (File.Create(probe)) { }
            File.Delete(probe);
        }
        catch (UnauthorizedAccessException)
        {
            result.AddError($"No write permission for directory '{path}'");
        }
    }
}