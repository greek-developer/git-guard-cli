using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;

namespace GitGuard.Config;

/// <summary><c>gitguard config path</c> and <c>gitguard config edit</c>.</summary>
internal static class ConfigCommand
{
    internal static Command Create()
    {
        var path = new Command("path", "Print the resolved config file path.");
        path.SetAction(RunPath);

        var edit = new Command("edit", "Open the config file in $VISUAL / $EDITOR.");
        edit.SetAction(RunEdit);

        return new Command("config", "Inspect and edit configuration.") { path, edit };
    }

    private static int RunPath(ParseResult _)
    {
        Console.WriteLine(ConfigurationManager.GetConfigPath());
        return 0;
    }

    private static int RunEdit(ParseResult _)
    {
        // Touching Config loads-or-creates the file with its defaults if it doesn't exist yet.
        _ = ConfigurationManager.Config;
        var path = ConfigurationManager.GetConfigPath();

        var editor = Environment.GetEnvironmentVariable("VISUAL")
            ?? Environment.GetEnvironmentVariable("EDITOR")
            ?? (OperatingSystem.IsWindows() ? "notepad" : "vi");

        try
        {
            // ArgumentList quotes each argument itself, so a path containing a quote or a space
            // survives intact instead of being re-split by a hand-built command line.
            var info = new ProcessStartInfo(editor) { UseShellExecute = false };
            info.ArgumentList.Add(path);

            using var process = Process.Start(info);
            if (process is null)
            {
                Console.Error.WriteLine($"Could not launch editor '{editor}'.");
                return 1;
            }

            process.WaitForExit();

            // The editor's own exit code is not our contract - a non-zero from it would otherwise
            // misleadingly read as a gitguard failure.
            if (process.ExitCode == 0)
            {
                return 0;
            }

            Console.Error.WriteLine($"editor exited with code {process.ExitCode}");
            return 1;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            Console.Error.WriteLine($"Could not launch editor '{editor}': {exception.Message}");
            return 1;
        }
    }
}
