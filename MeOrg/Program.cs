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

Task? liveMetricsTask = null;

if (args.Length > 0 && args[0] == "organize")
{
    liveMetricsTask = console.StartLiveMetrics(cts.Token);
}

Task writerBgTask = Task.Run(() => writer.WriteFilesContinuously(cts.Token));

RootCommand rootCommand = new("MeOrg is a media organizer CLI tool.");

rootCommand.Subcommands.Add(new OrganizeCommand(organizer));

ParseResult parseResult = rootCommand.Parse(args);

int exitCode = await parseResult.InvokeAsync(null, cts.Token);

writer.Shutdown();

await writerBgTask;

stopwatch.Stop();

cts.Cancel();

if (liveMetricsTask != null)
{
    await liveMetricsTask;
}

return exitCode;