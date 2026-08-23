namespace Hagalaz.Game.Messages
{
    public record WorldOfflineMessage(int Id, string InstanceId = "", long Generation = 0);
}
