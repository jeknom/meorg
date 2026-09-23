using System.CommandLine;
using MeOrg.Exceptions;

namespace MeOrg.Commands;

public class VerifyDirectorySyncedCommand : Command
{
    public VerifyDirectorySyncedCommand(CancellationToken cancellationToken) : base(
        "verify-sync",
        "Used to verify that a target directory contains all media files from a specified source.")
    {
        Option<DirectoryInfo> sourceDirOption = new("--source")
        {
            Description = "Source directory that contains media files.",
            Required = true
        };
        Options.Add(sourceDirOption);

        Option<DirectoryInfo> targetDirOption = new("--target")
        {
            Description = "Target directory that needs to contain all the files from the source directory.",
            Required = true
        };
        Options.Add(targetDirOption);

        SetAction(async (parseResult, ct) =>
        {
            var console = new SpectreConsole(yesToAll: false /* command does not have prompts, at least yet */);

            try
            {
                var fileAccess = new FileAccess();
                DirectoryEqualMediaComparer comparer = new(fileAccess, console);

                comparer.CompareMediaEquality(
                    parseResult.GetValue(sourceDirOption)!,
                    parseResult.GetValue(targetDirOption)!,
                    ct);

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
