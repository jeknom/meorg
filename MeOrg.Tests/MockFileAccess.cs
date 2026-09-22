namespace MeOrg.Tests;

public class MockFileAccess : IFileAccess
{
    private int amountOfCopyIOExcetionsQueued = 0;
    private int amountOfGenericExceptionQueued = 0;

    public void CopyFile(string sourceFileName, string destFileName)
    {
        if (amountOfGenericExceptionQueued > 0)
        {
            amountOfGenericExceptionQueued--;
            throw new Exception();
        }

        if (amountOfCopyIOExcetionsQueued > 0)
        {
            amountOfCopyIOExcetionsQueued--;
            throw new IOException();
        }
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

    public void QueueIOExceptionOnCopy(int amount)
    {
        amountOfCopyIOExcetionsQueued += amount;
    }

    public void QueueGenericExceptionOnCopy(int amount)
    {
        amountOfGenericExceptionQueued += amount;
    }

    // If this class is in use, it's assumed that there are no real files involved
    // thus, it is enough to just return the path itself
    public string GenerateSampledHashFromFile(string path)
    {
        return path;
    }
}
