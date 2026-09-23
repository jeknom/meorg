using MeOrg.Exceptions;

namespace MeOrg;

public interface IDirectoryEqualMediaComparer
{
    int CompareMediaEquality(DirectoryInfo sourceDir, DirectoryInfo targetDir, CancellationToken ct);
}

public class DirectoryEqualMediaComparer : IDirectoryEqualMediaComparer
{
    private readonly IFileAccess _fileAccess;
    private readonly IConsole _console;

    public DirectoryEqualMediaComparer(IFileAccess fileAccess, IConsole console)
    {
        _fileAccess = fileAccess;
        _console = console;
    }

    // Generates sampled hash for all media files in target dir
    // Checks that each hash in source can be found in target
    // Prints error and files which cannot be found in target
    public int CompareMediaEquality(DirectoryInfo sourceDir, DirectoryInfo targetDir, CancellationToken ct)
    {
        IEnumerable<string> targetPaths = Directory
            .EnumerateFiles(targetDir.FullName, "*", SearchOption.AllDirectories)
            .Where(FileHelper.IsSupportedMediaFileExtension);

        int targetMediaFileCount = 0;

        _console.WriteInfoLine("Generating sampled hash for all media files in target directory.");

        HashSet<string> targetHashes = new();
        foreach (string targetPath in targetPaths)
        {
            ct.ThrowIfCancellationRequested();

            targetMediaFileCount++;
            string targetFileHash = _fileAccess.GenerateSampledHashFromFile(targetPath);

            targetHashes.Add(targetFileHash);

            if (targetMediaFileCount % 50 == 0)
            {
                _console.WriteInfoLine($"'{targetMediaFileCount}' files hashed.");
            }
        }

        IEnumerable<string> sourcePaths = Directory
            .EnumerateFiles(sourceDir.FullName, "*", SearchOption.AllDirectories)
            .Where(FileHelper.IsSupportedMediaFileExtension);

        List<string> missingMediaFilepaths = new List<string>();
        int sourceMediaFileCount = 0;

        _console.WriteInfoLine("Checking source hashes against target.");

        foreach (string sourcePath in sourcePaths)
        {
            ct.ThrowIfCancellationRequested();

            sourceMediaFileCount++;
            string sourceFileHash = _fileAccess.GenerateSampledHashFromFile(sourcePath);

            if (!targetHashes.Contains(sourceFileHash))
            {
                missingMediaFilepaths.Add($"{sourcePath}");
            }

            if (sourceMediaFileCount % 50 == 0)
            {
                _console.WriteInfoLine($"'{sourceMediaFileCount}' files checked. '{missingMediaFilepaths.Count}' missing found so far.");
            }
        }

        _console.WriteInfoLine($"Source files total: '{sourceMediaFileCount}'\nTarget files total: '{targetMediaFileCount}'");

        if (missingMediaFilepaths.Count > 0)
        {
            throw new ErrorExitException(
                ExitCode.DirectoriesNotInSync,
                $"Missing following media files:\n{string.Join("\n", missingMediaFilepaths)}");
        }

        return 0;
    }
}
