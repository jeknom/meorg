using Spectre.Console;

namespace MeOrg;

public interface ISpectreConsole
{
    Task StartLiveMetrics(CancellationToken cancellationToken);
    void WriteError(string message);
    void WriteException(Exception ex);
}

public class SpectreConsole : ISpectreConsole
{
    private readonly OrganizeRunMetrics _metrics;
    private readonly Table _table;

    public SpectreConsole(OrganizeRunMetrics metrics)
    {
        _metrics = metrics;

        var table = new Table().RoundedBorder().BorderColor(Color.Aquamarine1).Title("Report");

        table.AddColumn("Metric");
        table.AddColumn("Value");

        table.AddRow("Pre-existing target directory lookup creation time", "TODO");
        table.AddRow("Pre-existing target media hash generation time", "TODO");
        table.AddRow("Source file processing time", "TODO");
        table.AddRow("Organized files", _metrics.CopyCount.ToString());
        table.AddRow("Duplicates filtered", _metrics.DuplicateCount.ToString());
        table.AddRow("Files with non-exif creation datetime", _metrics.NonExifCreationDateTimeCount.ToString());
        table.AddRow("Total seconds elapsed", _metrics.ElapsedSeconds.ToString());

        _table = table;
    }

    public void WriteError(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold red]✗ Error:[/] '{message}'");
    }

    public void WriteException(Exception ex)
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
    }

    public Task StartLiveMetrics(CancellationToken cancellationToken)
    {
        return AnsiConsole.Live(_table).StartAsync(async (context) =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UpdateTable();
                context.Refresh();

                await Task.Delay(500);
            }

            UpdateTable();
            context.Refresh();
        });
    }

    private void UpdateTable()
    {
        _table.Rows.Update(row: 0, column: 1, new Text(_metrics.PreExistingTargetDirLookupCreationTime != default ? $"{_metrics.PreExistingTargetDirLookupCreationTime.TotalSeconds}s" : "TODO"));
        _table.Rows.Update(row: 1, column: 1, new Text(_metrics.PreExistingTargetMediaHashGenerationTime != default ? $"{_metrics.PreExistingTargetMediaHashGenerationTime.TotalSeconds}s" : "TODO"));
        _table.Rows.Update(row: 2, column: 1, new Text(_metrics.SourceFileProcessingTime != default ? $"{_metrics.SourceFileProcessingTime.TotalSeconds}s" : "TODO"));
        _table.Rows.Update(row: 3, column: 1, new Text(_metrics.CopyCount.ToString()));
        _table.Rows.Update(row: 4, column: 1, new Text(_metrics.DuplicateCount.ToString()));
        _table.Rows.Update(row: 5, column: 1, new Text(_metrics.NonExifCreationDateTimeCount.ToString()));
        _table.Rows.Update(row: 6, column: 1, new Text($"{_metrics.ElapsedSeconds}s"));
    }
}