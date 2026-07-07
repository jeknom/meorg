using System.CommandLine;
using MeOrg.Extensions;

namespace MeOrg.Commands;

public class OrganizeCommand : Command
{
    public OrganizeCommand(IConsole console, CancellationToken cancellationToken) : base(
        "organize",
        "Used to organize media from an unorganized source directory into target directory.")
    {
        Option<DirectoryInfo> sourceDirOption = new("--source")
        {
            Description = "Unorganized media source directory.",
            Required = true
        };

        sourceDirOption.Validators.Add(result => result.IsDirectoryPathWithReadPermissions("source"));
        Options.Add(sourceDirOption);

        Option<DirectoryInfo> targetDirOption = new("--target")
        {
            Description = "Directory where to copy your organized media.",
            Required = true
        };
        Options.Add(targetDirOption);

        Option<bool> skipDedupe = new("--skip-dedupe")
        {
            Description = "Disables duplicate detection.",
            Required = false,
            DefaultValueFactory = _ => false
        };
        Options.Add(skipDedupe);

        Option<int> dayOffsetHours = new("--day-offset-hours")
        {
            Description = "Number of hours past midnight that still count as the previous day. With the default of 4, a photo taken at 3AM is filed under the previous day's directory instead of the current one.",
            Required = false,
            DefaultValueFactory = _ => 4,
        };
        dayOffsetHours.Validators.Add((result) =>
        {
            int value = result.GetValueOrDefault<int>();
            if (value < 0)
            {
                result.AddError($"--day-offset-hours must be >= 0, got {value}.");
            }
        });
        Options.Add(dayOffsetHours);

        SetAction(async (parseResult, ct) =>
        {
            var metrics = new OrganizeRunMetrics();
            var fileAccess = new FileAccess();
            var writer = new BackgroundFileWriter(metrics, console, fileAccess);

            IDuplicateFileDetector duplicateDetector = parseResult.GetValue(skipDedupe) ?
                new NoOpDuplicateFileDetector() :
                new DuplicateFileDetector(metrics, console);
            var organizer = new MediaOrganizer(writer, duplicateDetector, metrics, console, ct);

            await organizer.Organize(
                source: parseResult.GetValue(sourceDirOption)!,
                target: parseResult.GetValue(targetDirOption)!,
                dayOffset: TimeSpan.FromHours(parseResult.GetValue(dayOffsetHours))
            );

            return 0;
        });
    }
}