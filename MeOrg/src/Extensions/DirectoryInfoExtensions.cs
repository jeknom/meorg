namespace MeOrg.Extensions;

public static class DirectoryInfoExtensions
{
    public static bool HasPermissionToWrite(this DirectoryInfo dir)
    {
        string probePath = Path.Combine(dir.FullName, $".meorg-write-test-{Guid.NewGuid():N}");
        try
        {
            using (File.Create(probePath)) { }
        }
        catch (UnauthorizedAccessException)
        {
            return false;
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

        return true;
    }

    public static async Task PromptAndCreateIfMissingDirectory(
        this DirectoryInfo dir,
        IConsole console,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(dir.FullName) && await console.Confirm($"Path '{dir.FullName}' does not exist, create it?", cancellationToken))
        {
            Directory.CreateDirectory(dir.FullName);
        }
    }
}