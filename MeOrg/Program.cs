using System.CommandLine;
using System.Runtime.InteropServices;
using MeOrg;
using MeOrg.Commands;
using MeOrg.Exceptions;

using var cts = new CancellationTokenSource();
using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx =>
{
    ctx.Cancel = true;
    cts.Cancel();
});
using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx =>
{
    ctx.Cancel = true;
    cts.Cancel();
});

bool yesToAll = args.Any(arg => arg is "-y" or "--y");
var console = new SpectreConsole(yesToAll);
RootCommand rootCommand = new("MeOrg is a media organizer CLI tool.");

Option<bool> yta = new("--y")
{
    Description = "Answer yes to all prompts automatically. You can specify this after any command, not just the root command.",
    Required = false,
    DefaultValueFactory = _ => false
};
// Only added here so it shows up in help
rootCommand.Add(yta);

rootCommand.Subcommands.Add(new OrganizeCommand(console, cts.Token));

ParseResult parseResult = rootCommand.Parse(args);

try
{
    await parseResult.InvokeAsync(null, cts.Token);
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

return 0;