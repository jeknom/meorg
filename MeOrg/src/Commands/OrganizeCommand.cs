using System.CommandLine;
using MeOrg.Validators;

namespace MeOrg.Commands;

public class OrganizeCommand : Command
{
    private readonly IMediaOrganizer _organizer;

    public OrganizeCommand(IMediaOrganizer organizer) : base(
        "organize",
        "Used to organize media from an unorganized source directory into target directory.")
    {
        _organizer = organizer;

        Option<DirectoryInfo> sourceDirOption = new("--source")
        {
            Description = "Unorganized media source directory.",
            Required = true
        };

        sourceDirOption.Validators.Add(result => result.IsDirectoryPathWithReadPermissions());
        Options.Add(sourceDirOption);

        Option<DirectoryInfo> targetDirOption = new("--target")
        {
            Description = "Directory where to copy your organized media.",
            Required = true
        };
        targetDirOption.Validators.Add(result => result.IsDirectoryPathWithWritePermissions());
        Options.Add(targetDirOption);

        Option<bool> skipDedupe = new("--skip-dedupe")
        {
            Description = "Disables duplicate detection. Defaults to `false`.",
            Required = false
        };

        Option<int> dayOffsetHours = new("--day-offset-hours")
        {
            Description = "Groups media to previous day's directory within the given offset. Defaults to `4`.",
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

        SetAction((parseResult, ct) => OrganizeAction(
            sourceDir: parseResult.GetValue(sourceDirOption)!,
            targetDir: parseResult.GetValue(targetDirOption)!,
            dayOffsetHours: parseResult.GetValue(dayOffsetHours),
            skipDedupe: parseResult.GetValue(skipDedupe),
            cancellationToken: ct));
    }

    private async Task OrganizeAction(
        DirectoryInfo sourceDir,
        DirectoryInfo targetDir,
        int dayOffsetHours,
        bool skipDedupe,
        CancellationToken cancellationToken)
    {
        await _organizer.Organize(
            sourceDir,
            targetDir,
            dayOffset: TimeSpan.FromHours(dayOffsetHours),
            skipDedupe,
            cancellationToken);
    }
}