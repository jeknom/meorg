// MeOrg - deduplicates and organizes media files by creation date.
// Copyright (C) 2026 Johannes Palvanen
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using MeOrg.Exceptions;
using MeOrg.Extensions;
using System.Diagnostics;

namespace MeOrg;

public interface IMediaOrganizer
{
    Task Organize(DirectoryInfo source, DirectoryInfo target, TimeSpan dayOffset);
}

public class MediaOrganizer : IMediaOrganizer
{
    private readonly ParallelOptions _parallelOptions;
    private readonly OrganizeRunMetrics _metrics;
    private readonly IConsole _console;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private readonly IBackgroundFileWriter _writer;
    private readonly IDuplicateFileDetector _duplicateDetector;
    private readonly Dictionary<string, string> _suffixedTargetDirectoryLookup;
    private readonly CancellationToken _cancellationToken;

    public MediaOrganizer(
        IBackgroundFileWriter writer,
        IDuplicateFileDetector duplicateDetector,
        OrganizeRunMetrics metrics,
        IConsole console,
        CancellationToken cancellationToken)
    {
        _parallelOptions = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };
        _cancellationToken = cancellationToken;
        _suffixedTargetDirectoryLookup = new();
        _duplicateDetector = duplicateDetector;
        _metrics = metrics;
        _console = console;
        _writer = writer;
    }

    public async Task Organize(
        DirectoryInfo source,
        DirectoryInfo target,
        TimeSpan dayOffset)
    {
        await target.PromptAndCreateIfMissingDirectory(_console, _cancellationToken);
        if (!target.HasPermissionToWrite())
        {
            throw new ErrorExitException(ExitCode.PermissionDenied, "Missing permissions to write into the target directory.");
        }

        Task writerTask = Task.Run(() => _writer.WriteFilesContinuously(_cancellationToken), _cancellationToken);

        _stopwatch.Start();
        _console.WriteInfoLine("Populating pre-existing target directory lookup...");

        List<string> subDirNames = Directory
            .EnumerateDirectories(target.FullName, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList()!;

        var exactDirNames = new HashSet<string>(subDirNames, StringComparer.Ordinal);

        foreach (string dirName in subDirNames)
        {
            if (dirName.Length >= 10 && DateOnly.TryParseExact(dirName[..10], "yyyy-MM-dd", out _))
            {
                string prefix = dirName[..10];
                // If a directory matching the prefix exactly exists, it wins, don't redirect.
                if (!exactDirNames.Contains(prefix))
                {
                    _suffixedTargetDirectoryLookup.TryAdd(prefix, dirName);
                }
            }
        }

        _stopwatch.Restart();

        IEnumerable<string> targetMediaPaths = Directory
            .EnumerateFiles(target.FullName, "*", SearchOption.AllDirectories)
            .Where(FileHelper.IsSupportedMediaFileExtension);
        _duplicateDetector.MarkPathsAsSeen(targetMediaPaths);

        _metrics.ReportTargetMediaHashGenerationTime(_stopwatch.Elapsed);

        _console.WriteInfoLine("Filtering duplicate source media...");

        IEnumerable<string> filesPaths = Directory
            .EnumerateFiles(source.FullName, "*", SearchOption.AllDirectories)
            .Where(FileHelper.IsSupportedMediaFileExtension);
        filesPaths = _duplicateDetector.MarkAndReturnUnseen(filesPaths);

        if (filesPaths.Any() && !await _console.Confirm($"This operation will copy '{filesPaths.Count()}' files to target directory. Continue?", _cancellationToken))
        {
            _console.WriteInfoLine("Organize canceled, have a nice day!");
            return;
        }

        _console.WriteInfoLine("Copying files...");
        _stopwatch.Restart();

        await Parallel.ForEachAsync(
            filesPaths,
            _parallelOptions,
            (path, ct) => OrganizeFile(path, target, dayOffset, ct));

        _metrics.ReportSourceFileProcessingTime(_stopwatch.Elapsed);

        _console.WriteInfoLine("Wrapping up...");
        _stopwatch.Stop();

        _writer.Shutdown();

        await writerTask;

        _console.WriteReport(_metrics);
    }

    private async ValueTask OrganizeFile(
        string path,
        DirectoryInfo target,
        TimeSpan dayOffset,
        CancellationToken cancellationToken)
    {
        string groupName = Constants.DEFAULT_SUBDIR_NAME;

        if (FileHelper.TryExtractMediaMetadataCreationDateTime(path, _console, out DateTime estimatedCreationDateTime) ||
            FileHelper.TryExtractFileSystemGuesstimatedOriginalDateTime(path, _console, out estimatedCreationDateTime))
        {
            DateTime withOffset = estimatedCreationDateTime - dayOffset;
            if (estimatedCreationDateTime.Date != withOffset.Date)
            {
                estimatedCreationDateTime = withOffset;
            }

            groupName = estimatedCreationDateTime.ToMeorgDateString();
        }

        if (_suffixedTargetDirectoryLookup.TryGetValue(groupName, out string? suffixedSubDirName))
        {
            groupName = suffixedSubDirName;
        }

        string fileName = Path.GetFileName(path);
        string destinationPath = Path.Combine(target.FullName, groupName, fileName);
        await _writer.TryAddFile(fromPath: path, toPath: destinationPath, cancellationToken);
    }
}