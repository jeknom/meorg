using System.CommandLine;
using MeOrg.Validators;

namespace MeOrg.Commands;

public class OrganizeCommand : Command
{
    public OrganizeCommand() : base(
        "organize",
        "Used to organize media from an unorganized source directory into target directory.")
    {
        Option<DirectoryInfo> sourceDirOption = new("--source")
        {
            Description = "Unorganized media source directory.",
            Required = true
        };

        sourceDirOption.Validators.Add(result =>
        {
            result.NotEmptyString();
            result.IsDirectory();
            result.HasReadPermission();
        });
        Options.Add(sourceDirOption);

        Option<DirectoryInfo> targetDirOption = new("--target")
        {
            Description = "Directory where to copy your organized media.",
            Required = true
        };
        targetDirOption.Validators.Add(result =>
        {
            result.NotEmptyString();
            result.IsDirectory();
            result.HasWritePermission();
        });
        Options.Add(targetDirOption);


        SetAction((parseResult) => OrganizeAction(
            sourceDir: parseResult.GetValue(sourceDirOption)!,
            targetDir: parseResult.GetValue(targetDirOption)!));
    }

    private static void OrganizeAction(DirectoryInfo sourceDir, DirectoryInfo targetDir)
    {
        // 1. Recursively iterate through source directory
        // 2. When encountering any of the specified files of type, enqueue them to bounded channel or concurrent queue (what is the difference?)
        // 3. Queue could live in its own class and have a continious task that writes files
        // 4. Profit?
    }
}