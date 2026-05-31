using System.CommandLine;
using System.Runtime.InteropServices;
using MeOrg.Commands;

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

RootCommand rootCommand = new("MeOrg is a media organizer CLI tool.");

rootCommand.Subcommands.Add(new OrganizeCommand());

ParseResult parseResult = rootCommand.Parse(args);

int exitCode = await parseResult.InvokeAsync(null, cts.Token);

cts.Cancel();

return exitCode;