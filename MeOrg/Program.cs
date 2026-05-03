using System.CommandLine;
using System.Runtime.InteropServices;
using MeOrg;
using MeOrg.Commands;
using Microsoft.Extensions.Logging;

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

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = factory.CreateLogger<Program>();

var writer = new BackgroundFileWriter();

Task writerBgTask = Task.Run(() => writer.WriteFilesContiniously(cts.Token));

RootCommand rootCommand = new("MeOrg is a media organizer CLI tool.");

rootCommand.Subcommands.Add(new OrganizeCommand(writer, logger));

ParseResult parseResult = rootCommand.Parse(args);

int exitCode = await parseResult.InvokeAsync(null, cts.Token);

writer.Shutdown();

await writerBgTask;

return exitCode;