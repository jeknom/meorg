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

        SetAction((parseResult, ct) => OrganizeAction(
            sourceDir: parseResult.GetValue(sourceDirOption)!,
            targetDir: parseResult.GetValue(targetDirOption)!,
            skipDedupe: parseResult.GetValue(skipDedupe),
            cancellationToken: ct));
    }

    private async Task OrganizeAction(
        DirectoryInfo sourceDir,
        DirectoryInfo targetDir,
        bool skipDedupe,
        CancellationToken cancellationToken)
    {
        await _organizer.Organize(sourceDir, targetDir, skipDedupe, cancellationToken);
    }
}