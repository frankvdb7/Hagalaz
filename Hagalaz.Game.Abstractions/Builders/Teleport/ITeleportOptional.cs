using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Teleport
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITeleportOptional : ITeleportBuild
    {
        ITeleportOptional WithZ(int z);
        ITeleportOptional WithDimension(int dimension);
    }
}