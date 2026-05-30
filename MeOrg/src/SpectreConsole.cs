using Spectre.Console;

namespace MeOrg;

public interface ISpectreConsole
{
    void WriteInfoLine(string message);
    void WriteErrorLine(string message);
    void WriteException(Exception ex);
    void WriteInputs(string source, string target, int dayOffset, bool dedupe, bool yesToAll);
    Task<bool> Confirm(string question, CancellationToken cancellationToken);
}

public class SpectreConsole : ISpectreConsole
{
    private readonly OrganizeRunMetrics _metrics;

    public SpectreConsole(OrganizeRunMetrics metrics)
    {
        _metrics = metrics;
    }

    public Task<bool> Confirm(string question, CancellationToken cancellationToken)
    {
        return AnsiConsole.ConfirmAsync($"{DateTime.UtcNow} - [bold yellow] Question:[/] '{question}'", defaultValue: false, cancellationToken);
    }

    public void WriteInfoLine(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"{DateTime.UtcNow} - [bold cyan] Info:[/] '{message}'");
    }

    public void WriteErrorLine(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"{DateTime.UtcNow} - [bold red]✗ Error:[/] '{message}'");
    }

    public void WriteException(Exception ex)
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
    }

    public void WriteInputs(string source, string target, int dayOffset, bool dedupe, bool yesToAll)
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Green1).Title("Inputs");
        table.AddColumn("Input");
        table.AddColumn("Value");

        table.AddRow("Source", source);
        table.AddRow("Target", target);
        table.AddRow("Day offset hours", dayOffset.ToString());
        table.AddRow("Dedupe media", dedupe ? "Yes" : "No");
        table.AddRow("Say yes to all prompts", yesToAll ? "Yes" : "Prompt user");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public void WriteReport()
    {
        var table = new Table().RoundedBorder().BorderColor(Color.Aquamarine1).Title("Report");

        table.AddColumn("Metric");
        table.AddColumn("Value");

        table.AddRow("Pre-existing target directory lookup creation time", _metrics.PreExistingTargetDirLookupCreationTime != default ? $"{_metrics.PreExistingTargetDirLookupCreationTime.TotalSeconds}s" : "Not reported");
        table.AddRow("Pre-existing target media hash generation time", _metrics.PreExistingTargetMediaHashGenerationTime != default ? $"{_metrics.PreExistingTargetMediaHashGenerationTime.TotalSeconds}s" : "Not reported");
        table.AddRow("Source file processing time", _metrics.SourceFileProcessingTime != default ? $"{_metrics.SourceFileProcessingTime.TotalSeconds}s" : "Not reported");
        table.AddRow("Organized files", _metrics.CopyCount.ToString());
        table.AddRow("Duplicates filtered", _metrics.DuplicateCount.ToString());
        table.AddRow("Files with non-exif creation datetime", _metrics.NonExifCreationDateTimeCount.ToString());
        table.AddRow("Total seconds elapsed", _metrics.ElapsedSeconds.ToString());

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}