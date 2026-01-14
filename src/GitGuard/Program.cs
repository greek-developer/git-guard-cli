using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Runtime.CompilerServices;

using GitGuard.Config;
using GitGuard.Commands;

namespace GitGuard;

public class Program
{
    public static Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("gitguard: A tool to manage multiple repositories.");

        var getConfigPathCommand = new Command(
                "get-config-path",
                "Displays the full path to the config file (in user's profile)");

        getConfigPathCommand.SetAction(_ => Console.WriteLine(ConfigurationManager.GetConfigPath()));

        rootCommand.Add(getConfigPathCommand);     
    
        AddCommands(rootCommand, FolderCommands.GenerateFolderCommands());
        AddCommands(rootCommand, RepositoriesCommands.GenerateRepositoriesCommands());

        return rootCommand
            .Parse(args)
            .InvokeAsync();
    }

    private static void AddCommands(RootCommand rootCommand, IEnumerable<Command> commands)
    {
        foreach (var command in commands)
        {
            rootCommand.Add(command);
        }
    }
}