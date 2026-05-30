using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MeOrg;
using MeOrg.Commands;

var stopwatch = new Stopwatch();
stopwatch.Start();

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

var metrics = new OrganizeRunMetrics();
var console = new SpectreConsole(metrics);
var writer = new BackgroundFileWriter(metrics, console);
var organizer = new MediaOrganizer(writer, metrics, console, cts.Token);

RootCommand rootCommand = new("MeOrg is a media organizer CLI tool.");

rootCommand.Subcommands.Add(new OrganizeCommand(organizer));

ParseResult parseResult = rootCommand.Parse(args);

int exitCode = await parseResult.InvokeAsync(null, cts.Token);

stopwatch.Stop();

cts.Cancel();

return exitCode;