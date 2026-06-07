using Spectre.Console;

namespace MeOrg;

public class SpectreConsole : IConsole
{
    private readonly bool _yesToAll;

    public SpectreConsole(bool yesToAll)
    {
        _yesToAll = yesToAll;
    }

    public Task<bool> Confirm(string question, CancellationToken cancellationToken)
    {
        if (_yesToAll)
        {
            return Task.FromResult(true);
        }

        return AnsiConsole.ConfirmAsync($"{DateTime.UtcNow} - [bold yellow] Question:[/] '{Markup.Escape(question)}'", defaultValue: false, cancellationToken);
    }

    public void WriteInfoLine(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"{DateTime.UtcNow} - [bold cyan] Info:[/] '{Markup.Escape(message)}'");
    }

    public void WriteErrorLine(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"{DateTime.UtcNow} - [bold red]✗ Error:[/] '{Markup.Escape(message)}'");
    }

    public void WriteException(Exception ex)
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
    }

    public void WriteInputs(string source, string target, int dayOffset, bool dedupe, bool promptUser)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Green1).Title("Inputs");
        table.AddColumn("Input");
        table.AddColumn("Value");

        table.AddRow("Source", Markup.Escape(source));
        table.AddRow("Target", Markup.Escape(target));
        table.AddRow("Day offset hours", dayOffset.ToString());
        table.AddRow("Dedupe media", dedupe ? "Yes" : "No");
        table.AddRow("Prompt user", promptUser ? "Yes" : "No");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public void WriteReport(OrganizeRunMetrics metrics)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Aquamarine1).Title("Report");

        table.AddColumn("Metric");
        table.AddColumn("Value");

        table.AddRow("Duplicates", metrics.DuplicateCount.ToString());
        table.AddRow("Copied media", metrics.CopyCount.ToString());
        table.AddRow("Target hashing time", metrics.TargetMediaHashGenerationTime != default ? $"{metrics.TargetMediaHashGenerationTime.TotalSeconds}s" : "Not reported");
        table.AddRow("Source processing time", metrics.SourceFileProcessingTime != default ? $"{metrics.SourceFileProcessingTime.TotalSeconds}s" : "Not reported");
        table.AddRow("Total elapsed time", $"{metrics.ElapsedSeconds}s");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}