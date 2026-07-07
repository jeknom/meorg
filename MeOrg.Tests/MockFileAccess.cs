namespace MeOrg.Tests;

public class MockFileAccess : IFileAccess
{
    public void CopyFile(string sourceFileName, string destFileName)
    {
    }

    public void CreateDirectory(string path)
    {
    }

    public bool DirectoryExists(string? path)
    {
        return false;
    }

    public bool FileExists(string? path)
    {
        return false;
    }
}