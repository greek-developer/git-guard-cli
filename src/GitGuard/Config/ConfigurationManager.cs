
using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;


namespace GitGuard.Config;

public static class ConfigurationManager
{

    private static GitGuardConfig? _config;
    
    public static GitGuardConfig Config { get => _config ??= LoadConfig(); }

    public static string GetConfigPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".gitguard",
            "config.json");

    private static GitGuardConfig LoadConfig()
    {
        var configPath = GetConfigPath();

        if (!File.Exists(configPath))
        {
            var configDir = Path.GetDirectoryName(configPath);

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir!);
            }

            var defaultConfig = new GitGuardConfig();
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(defaultConfig, jsonOptions);
            File.WriteAllText(configPath, json);
            return defaultConfig;
        }
        else
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<GitGuardConfig>(json);
            return config ?? new GitGuardConfig();
        }
    }

    public static void SaveConfig()
    {
        var configPath = GetConfigPath();
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var json = JsonSerializer.Serialize(_config, jsonOptions);
        File.WriteAllText(configPath, json);
    }
}

