using System.CommandLine;
using MeOrg.Exceptions;

namespace MeOrg.Commands;

public class CountMediaFilesCommand : Command
{
    public CountMediaFilesCommand() : base(
        "count-media",
        "Used to count how many media files there are in a given directory.")
    {
        Option<DirectoryInfo> directory = new("--dir")
        {
            Description = "Directory to count.",
            Required = true
        };
        Options.Add(directory);

        SetAction(async (parseResult, ct) =>
        {
            var console = new SpectreConsole(yesToAll: false /* command does not have prompts, at least yet */);
            DirectoryInfo dir = parseResult.GetValue(directory)!;

            try
            {
                console.WriteInfoLine("Starting the count.");

                int count = Directory
                    .EnumerateFiles(dir.FullName, "*", SearchOption.AllDirectories)
                    .Count(FileHelper.IsSupportedMediaFileExtension);

                console.WriteInfoLine($"There are '{count}' media files in that directory.");

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
