using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Npc
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface INpcId
    {
        INpcLocation WithId(int id);
    }
}