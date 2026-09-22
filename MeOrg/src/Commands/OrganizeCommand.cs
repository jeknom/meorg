using System.CommandLine;
using MeOrg.Exceptions;
using MeOrg.Extensions;

namespace MeOrg.Commands;

public class OrganizeCommand : Command
{
    public OrganizeCommand(CancellationToken cancellationToken) : base(
        "organize",
        "Used to organize media from an unorganized source directory into target directory.")
    {
        Option<DirectoryInfo> sourceDirOption = new("--source")
        {
            Description = "Unorganized media source directory.",
            Required = true
        };

        sourceDirOption.Validators.Add(result => result.IsDirectoryPathWithReadPermissions("source"));
        Options.Add(sourceDirOption);

        Option<DirectoryInfo> targetDirOption = new("--target")
        {
            Description = "Directory where to copy your organized media.",
            Required = true
        };
        Options.Add(targetDirOption);

        Option<bool> skipDedupe = new("--skip-dedupe")
        {
            Description = "Disables duplicate detection.",
            Required = false,
            DefaultValueFactory = _ => false
        };
        Options.Add(skipDedupe);

        Option<bool> yesToAll = new("--yes-to-all", "-y")
        {
            Description = "Automatically answer yes to all prompts.",
            Required = false,
            DefaultValueFactory = _ => false
        };
        Options.Add(yesToAll);

        SetAction(async (parseResult, ct) =>
        {
            var console = new SpectreConsole(yesToAll: parseResult.GetValue(yesToAll));

            try
            {
                var metrics = new OrganizeRunMetrics();
                var fileAccess = new FileAccess();
                var writer = new BackgroundFileWriter(metrics, console, fileAccess, Constants.DEFAULT_BACKGROUND_FILE_WRITER_OPTIONS);

                IDuplicateFileDetector duplicateDetector = parseResult.GetValue(skipDedupe) ?
                    new NoOpDuplicateFileDetector() :
                    new DuplicateFileDetector(metrics, fileAccess, console);
                var organizer = new MediaOrganizer(writer, duplicateDetector, metrics, console, ct);

                await organizer.Organize(
                    source: parseResult.GetValue(sourceDirOption)!,
                    target: parseResult.GetValue(targetDirOption)!
                );

                return 0;
            }
            catch (OperationCanceledException)
            {
                console.WriteErrorLine($"Error code: {(int)ExitCode.Cancelled} ({ExitCode.Cancelled}) Message: Cancelled");
                return (int)ExitCode.Cancelled;
            }
            catch (ErrorExitException exitException)
            {
                console.WriteErrorLine($"Error code: {(int)exitException.Code} ({exitException.Code}) Message: {exitException.Message}");
                return (int)exitException.Code;
            }
            catch (Exception ex)
            {
                console.WriteException(ex);
                return (int)ExitCode.Unexpected;
            }
        });
    }
}
