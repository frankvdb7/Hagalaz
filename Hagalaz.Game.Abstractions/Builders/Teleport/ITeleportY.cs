using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Teleport
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITeleportY
    {
        ITeleportOptional WithY(int y);
    }
}