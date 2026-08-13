using System.CommandLine;
using System.Text;

using GitGuard.Commands;
using GitGuard.Config;
using GitGuard.Skills;
using GitGuard.Versioning;

namespace GitGuard;

public class Program
{
    public static Task<int> Main(string[] args)
    {
        UseUtf8Output();

        var rootCommand = new RootCommand("gitguard: A tool to manage multiple repositories.");

        var getConfigPathCommand = new Command(
                "get-config-path",
                "Displays the full path to the config file (in user's profile)");

        getConfigPathCommand.SetAction(_ => Console.WriteLine(ConfigurationManager.GetConfigPath()));

        rootCommand.Add(getConfigPathCommand);

        AddCommands(rootCommand, FolderCommands.GenerateFolderCommands());
        AddCommands(rootCommand, RepositoriesCommands.GenerateRepositoriesCommands());

        rootCommand.Add(VersionCommand.Create());
        rootCommand.Add(SkillCommand.Create());

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

    /// <summary>
    /// Windows consoles default to a legacy code page, which silently transliterates anything
    /// outside it - em dashes, arrows and emoji all survive on screen but not through a
    /// redirect. Repository paths, remote URLs and the emitted skill file all carry such
    /// characters, so the output encoding is pinned before anything is written.
    /// </summary>
    private static void UseUtf8Output()
    {
        try
        {
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }
        catch (IOException)
        {
            // Some redirected handles refuse the change; plain ASCII output still works.
        }
    }
}
