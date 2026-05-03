namespace MeOrg.Validators;

using System.CommandLine.Parsing;

public static class OptionParseResultExtensions
{
    public static void IsDirectoryPathWithReadPermissions(this OptionResult result)
    {
        if (result.Tokens.Count == 0)
        {
            result.AddError("Source directory path not specified");
            return;
        }

        string dirPath = result.Tokens.Single().Value;
        if (!Directory.Exists(dirPath))
        {
            result.AddError($"Source directory '{dirPath}' does not exist");
            return;
        }

        try
        {
            _ = Directory.EnumerateFileSystemEntries(dirPath).GetEnumerator().MoveNext();
        }
        catch (UnauthorizedAccessException)
        {
            result.AddError($"No read permission for directory '{dirPath}'");
        }
    }

    public static void IsDirectoryPathWithWritePermissions(this OptionResult result)
    {
        if (result.Tokens.Count == 0)
        {
            result.AddError("Source directory path not specified");
            return;
        }

        string dirPath = result.Tokens.Single().Value;
        if (!Directory.Exists(dirPath))
        {
            result.AddError($"Source directory '{dirPath}' does not exist");
            return;
        }

        string probe = Path.Combine(dirPath, $".meorg-write-test-{Guid.NewGuid():N}");
        try
        {
            using (File.Create(probe)) { }
            File.Delete(probe);
        }
        catch (UnauthorizedAccessException)
        {
            result.AddError($"No write permission for directory '{dirPath}'");
        }
    }
}