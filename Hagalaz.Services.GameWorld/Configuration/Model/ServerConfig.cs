namespace Hagalaz.Services.GameWorld.Configuration.Model;

/// <summary>
/// 
/// </summary>
public record ServerConfig
{
    public const string Key = "Configuration";
    public int ClientRevision { get; init; }
    public int ClientRevisionPatch { get; init; }
}