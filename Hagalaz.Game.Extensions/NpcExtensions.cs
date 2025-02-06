using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Extensions
{
    public static class NpcExtensions
    {
        public static bool HasDisplayName(this INpc npc) => !string.IsNullOrWhiteSpace(npc.DisplayName) && npc.Name != npc.DisplayName;
    }
}
