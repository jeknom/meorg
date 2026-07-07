namespace MeOrg;

public interface IFileAccess
{
    bool DirectoryExists(string? path);
    bool FileExists(string? path);
    void CopyFile(string sourceFileName, string destFileName);
    void CreateDirectory(string path);
}

public class FileAccess : IFileAccess
{
    public void CopyFile(string sourceFileName, string destFileName)
    {
        File.Copy(sourceFileName, destFileName);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public bool DirectoryExists(string? path)
    {
        return Directory.Exists(path);
    }

    public bool FileExists(string? path)
    {
        return File.Exists(path);
    }
}