using System.CommandLine;
using MeOrg.Commands;

RootCommand rootCommand = new("MeOrg is a media organizer CLI tool.");

rootCommand.Subcommands.Add(new OrganizeCommand());

ParseResult parseResult = rootCommand.Parse(args);

return parseResult.Invoke();