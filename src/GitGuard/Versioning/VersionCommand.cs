using System.CommandLine;

namespace GitGuard.Versioning;

internal static class VersionCommand
{
    internal static Command Create()
    {
        var command = new Command("version", "Print the version, commit and build time of this build");
        command.SetAction(Run);
        return command;
    }

    private static int Run(ParseResult _)
    {
        var path = Path.Combine(AppContext.BaseDirectory, ProductionVersion.FileName);

        if (!File.Exists(path))
        {
            // Diagnostics go to stderr so redirecting stdout still yields only the version block.
            Console.Error.WriteLine(
                $"{ProductionVersion.FileName} is missing from '{AppContext.BaseDirectory}'. It is written at build time.");
            return 1;
        }

        var version = ProductionVersion.Parse(File.ReadAllText(path));
        if (!version.IsValid)
        {
            foreach (var error in version.Errors)
            {
                Console.Error.WriteLine(error);
            }

            return 1;
        }

        Console.Out.WriteLine(version.Value!.ToDisplay());
        Console.Out.WriteLine();
        Console.Out.WriteLine("To update: dotnet tool update -g grdev.git-guard-cli");
        return 0;
    }
}
