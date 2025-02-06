namespace Hagalaz.Game.Abstractions.Builders.Item
{
    public interface IItemOptional : IItemBuild
    {
        IItemOptional WithCount(int count);
        IItemOptional WithExtraData(string data);
    }
}