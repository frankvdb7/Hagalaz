using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Builders.Teleport
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITeleportBuild
    {
        ITeleport Build();
    }
}