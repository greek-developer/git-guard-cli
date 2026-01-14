using System.Text.Json.Serialization;

namespace GitGuard.Config;

public class GitGuardConfig
{
 [JsonPropertyName("folders")]
    public List<MonitoredFolder> Folders { get; set; } = new();
}

public class MonitoredFolder 
{  
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; } = string.Empty;   
}