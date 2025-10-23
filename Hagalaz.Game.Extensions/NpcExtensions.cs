using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="INpc"/> interface.
    /// </summary>
    public static class NpcExtensions
    {
        /// <summary>
        /// Checks if the NPC has a custom display name that is different from its default name.
        /// </summary>
        /// <param name="npc">The NPC to check.</param>
        /// <returns><c>true</c> if the NPC has a non-empty, non-whitespace display name that differs from its default name; otherwise, <c>false</c>.</returns>
        public static bool HasDisplayName(this INpc npc) => !string.IsNullOrWhiteSpace(npc.DisplayName) && npc.Name != npc.DisplayName;
    }
}
