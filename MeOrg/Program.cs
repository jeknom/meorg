using System.CommandLine;
using System.CommandLine.Parsing;

RootCommand rootCommand = new("MeOrg is a media organizer CLI tool.");

Command organizeCommand = new("organize")
{
    Description = "Used to organize media from an unorganized source directory into target directory."
};
Option<DirectoryInfo> sourceDirOption = new("--source")
{
    Description = "Unorganized media source directory.",
    Required = true,
    DefaultValueFactory = result =>
    {
        if (result.Tokens.Count == 0)
        {
            result.AddError("Source directory path not specified");
        }

        string dirPath = result.Tokens.Single().Value;
        if (!Directory.Exists(dirPath))
        {
            result.AddError("Source directory does not exist");
        }

        return new DirectoryInfo(dirPath);
    }
};
organizeCommand.Options.Add(sourceDirOption);
organizeCommand.SetAction((parseResult) => Organize(parseResult.GetValue(sourceDirOption)!));

rootCommand.Subcommands.Add(organizeCommand);

ParseResult parseResult = rootCommand.Parse(args);

return parseResult.Invoke();

static void Organize(DirectoryInfo sourceDir)
{
    Console.WriteLine($"Source dir is {sourceDir.Name}");
}