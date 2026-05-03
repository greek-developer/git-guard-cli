using System.CommandLine;
using System.Net.NetworkInformation;
using LibGit2Sharp;

namespace GitGuard.Commands;

public static class RepositoriesCommands
{

    public static IEnumerable<Command> GenerateRepositoriesCommands()
    {
        var repositoriesScanCommand = new Command("scan", "scan monitored folders for repositories");

        repositoriesScanCommand.Options.Add(new Option<string[]>("--path-filter-include", "-pfi"));
        repositoriesScanCommand.Options.Add(new Option<string[]>("--path-filter-exclude", "-pfe"));
        repositoriesScanCommand.Options.Add(new Option<string[]>("--origin-filter-include", "-ofi"));
        repositoriesScanCommand.Options.Add(new Option<string[]>("--origin-filter-exclude", "-ofe"));

        repositoriesScanCommand.SetAction( pr =>
        {
            var originFilterInclude = pr.GetValue<string[]>("--origin-filter-include") ?? Array.Empty<string>();
            var originFilterExclude = pr.GetValue<string[]>("--origin-filter-exclude") ?? Array.Empty<string>();
            var pathFilterInclude = pr.GetValue<string[]>("--path-filter-include") ?? Array.Empty<string>();
            var pathFilterExclude = pr.GetValue<string[]>("--path-filter-exclude") ?? Array.Empty<string>();

            var repositories = RepositoryManager
                .Repositories
                .Where(repo => originFilterInclude.Length == 0 || originFilterInclude.Any(f => repo.repository.OriginContains(f)))                
                .Where(repo => originFilterExclude.Length == 0 || !originFilterExclude.Any(f => repo.repository.OriginContains(f)))
                .Where(repo => pathFilterInclude.Length == 0 || pathFilterInclude.Any(f => repo.path.Contains(f, StringComparison.OrdinalIgnoreCase)))
                .Where(repo => pathFilterExclude.Length == 0 || !pathFilterExclude.Any(f => repo.path.Contains(f, StringComparison.OrdinalIgnoreCase)));

            Console.WriteLine("");
            Console.WriteLine("Repositories:");
            Console.WriteLine("");
            Console.WriteLine(
                string.Join(
                    Environment.NewLine, 
                    repositories.Select(repo => GetRepositoryDescription(repo))));            
            Console.WriteLine("");
        });

        var RepositoriesCommand = new Command("repositories", "Manage repositories in monitored folders")
        {
            repositoriesScanCommand,
        };

        return new[] { RepositoriesCommand };
    }      

    static string GetRepositoryDescription((string path, LibGit2Sharp.Repository repository) repo)
    {
        var status = repo.repository.RetrieveStatus(new StatusOptions());        
        return $"[{(status.IsDirty ? '+' : ' ')}]  {repo.path} => {repo.repository.Network.Remotes["origin"]?.Url}";
    }

    static bool OriginContains(LibGit2Sharp.Repository repo, string filter)
    {
        return repo.Network.Remotes["origin"]?.Url?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true;
    }
}

public static class RepositoryExtensions
{
    public static bool OriginContains(this Repository repo, string substring)
    {
        return repo.Network.Remotes["origin"]?.Url?.Contains(substring, StringComparison.OrdinalIgnoreCase) == true;
    }
}