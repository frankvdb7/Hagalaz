using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Builders.GroundItem
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGroundItemBuild
    {
        IGroundItem Build();
        IGroundItem Spawn();
    }
}