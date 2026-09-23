using System.CommandLine;
using MeOrg.Exceptions;

namespace MeOrg.Commands;

public class HashCompareCommand : Command
{
    public HashCompareCommand(CancellationToken cancellationToken) : base(
        "compare-hash",
        "Used to compare hash generation for two files.")
    {
        Option<FileInfo> fileAOption = new("-a")
        {
            Description = "File A.",
            Required = true
        };
        Options.Add(fileAOption);

        Option<FileInfo> fileBOption = new("-b")
        {
            Description = "File B.",
            Required = true
        };
        Options.Add(fileBOption);

        SetAction(async (parseResult, ct) =>
        {
            var console = new SpectreConsole(yesToAll: false /* command does not have prompts, at least yet */);

            try
            {
                var fileAccess = new FileAccess();
                string hashA = fileAccess.GenerateSampledHashFromFile(parseResult.GetValue(fileAOption)!.FullName);
                string hashB = fileAccess.GenerateSampledHashFromFile(parseResult.GetValue(fileBOption)!.FullName);

                if (hashA != hashB)
                {
                    throw new ErrorExitException(ExitCode.HashNotEqual, $"Hashes are not equal. Hash A = '{hashA}', Hash B = '{hashB}'");
                }

                console.WriteInfoLine($"Hashes are equal. Hash A = '{hashA}', Hash B = '{hashB}'");

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
