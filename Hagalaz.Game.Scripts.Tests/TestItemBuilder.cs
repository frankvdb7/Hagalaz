using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Tests;

internal sealed class TestItemBuilder(Func<int, int, IItem> createItem) : IItemBuilder, IItemId, IItemOptional
{
    private int _id;
    private int _count = 1;

    public IItemId Create() => this;

    public IItemOptional WithId(int id)
    {
        _id = id;
        return this;
    }

    public IItemOptional WithCount(int count)
    {
        _count = count;
        return this;
    }

    public IItemOptional WithExtraData(string data) => this;

    public IItem Build() => createItem(_id, _count);
}
