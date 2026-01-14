using System.CommandLine;
using System.Net.NetworkInformation;
using LibGit2Sharp;

namespace GitGuard.Commands;

public static class RepositoriesCommands
{

    public static IEnumerable<Command> GenerateRepositoriesCommands()
    {
        var repositoriesScanCommand = new Command("scan", "scan monitored folders for repositories");
        repositoriesScanCommand.SetAction(_ =>
        {
            Console.WriteLine("");
            Console.WriteLine("Repositories:");
            Console.WriteLine("");
            Console.WriteLine(
                string.Join(
                    Environment.NewLine, 
                    RepositoryManager.Repositories.Select(repo => GetRepositoryDescription(repo))));            
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
}