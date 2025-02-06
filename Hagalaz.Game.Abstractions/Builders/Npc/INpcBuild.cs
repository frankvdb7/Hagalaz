using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Builders.Npc
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface INpcBuild
    {
        INpc Build();
        INpcHandle Spawn();
    }
}