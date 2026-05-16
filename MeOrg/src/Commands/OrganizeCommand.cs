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


        SetAction((parseResult, ct) => OrganizeAction(
            sourceDir: parseResult.GetValue(sourceDirOption)!,
            targetDir: parseResult.GetValue(targetDirOption)!,
            cancellationToken: ct));
    }

    private async Task OrganizeAction(
        DirectoryInfo sourceDir,
        DirectoryInfo targetDir,
        CancellationToken cancellationToken)
    {
        await _organizer.Organize(sourceDir, targetDir, cancellationToken);
    }
    // 2. When encountering any of the specified files of type, enqueue them to bounded channel or concurrent queue (what is the difference?)

    // 3. Queue could live in its own class and have a continious task that writes files
    // 4. Profit?    
}