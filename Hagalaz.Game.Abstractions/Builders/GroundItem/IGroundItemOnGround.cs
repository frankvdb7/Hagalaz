using System;
using System.ComponentModel;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Builders.GroundItem
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGroundItemOnGround
    {
        IGroundItemLocation WithItem(IItem item);
        IGroundItemLocation WithItem(Func<IItemBuilder, IItemBuild> itemBuilder);
    }
}