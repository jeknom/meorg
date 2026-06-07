namespace MeOrg.Extensions;

using System.CommandLine.Parsing;

public static class OptionParseResultExtensions
{
    public static void IsDirectoryPathWithReadPermissions(this OptionResult result, string kind)
    {
        if (result.Tokens.Count == 0)
        {
            result.AddError($"'{kind}' directory path not specified");
            return;
        }

        string dirPath = result.Tokens.Single().Value;
        if (!Directory.Exists(dirPath))
        {
            result.AddError($"'{kind}' directory '{dirPath}' does not exist");
            return;
        }

        try
        {
            using var enumerator = Directory.EnumerateFileSystemEntries(dirPath).GetEnumerator();
            enumerator.MoveNext();
        }
        catch (UnauthorizedAccessException)
        {
            result.AddError($"No read permission for '{kind}' directory '{dirPath}'");
        }
    }

    public static void IsDirectoryPathWithWritePermissions(this OptionResult result, string kind)
    {
        if (result.Tokens.Count == 0)
        {
            result.AddError($"'{kind}' directory path not specified");
            return;
        }

        string dirPath = result.Tokens.Single().Value;
        if (!Directory.Exists(dirPath))
        {
            result.AddError($"'{kind}' directory '{dirPath}' does not exist");
            return;
        }

        string probePath = Path.Combine(dirPath, $".meorg-write-test-{Guid.NewGuid():N}");
        try
        {
            using (File.Create(probePath)) { }
        }
        catch (UnauthorizedAccessException)
        {
            result.AddError($"No write permission for '{kind}' directory '{dirPath}'");
        }
        finally
        {
            try
            {
                File.Delete(probePath);
            }
            catch
            {
                // Doing my best to clean it up
            }
        }
    }
}