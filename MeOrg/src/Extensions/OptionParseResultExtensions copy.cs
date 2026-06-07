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
}