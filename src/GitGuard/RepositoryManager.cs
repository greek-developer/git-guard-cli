using LibGit2Sharp;
using GitGuard.Config;

namespace GitGuard;

public static class RepositoryManager
{
    public static List<(string path, Repository repository)> Repositories { get; private set; } 

    static RepositoryManager()
    {
        Repositories = ScanFolderForRepositories();
    }

    private static List<( string path, Repository repository)> ScanFolderForRepositories()
    {        
        var config = ConfigurationManager.Config;

        return config
            .Folders
            .SelectMany(folder =>Directory.GetDirectories(folder.Path, ".git", SearchOption.AllDirectories))
            .Select(gitFolderPath => Path.GetDirectoryName(gitFolderPath) ?? string.Empty)
            .Where(path => !string.IsNullOrEmpty(path))
            .OrderBy(path => path)
            .Select(path => (path, new Repository(path)))
            .ToList();
    }
}