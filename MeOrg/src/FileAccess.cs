namespace MeOrg;

using MeOrg.Extensions;

public interface IFileAccess
{
    bool DirectoryExists(string? path);
    bool FileExists(string? path);
    void CopyFile(string sourceFileName, string destFileName);
    void CreateDirectory(string path);
    string GenerateSampledHashFromFile(string path);
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

    public string GenerateSampledHashFromFile(string path)
    {
        using FileStream fileStream = File.OpenRead(path);
        string hash = fileStream.GenerateSampledHash();

        return hash;
    }
}
