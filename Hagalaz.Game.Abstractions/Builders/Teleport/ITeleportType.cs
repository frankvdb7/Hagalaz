using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Teleport
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITeleportType
    {
        ITeleportOptional WithLocation(ILocation location);
        ITeleportY WithX(int x);
    }
}