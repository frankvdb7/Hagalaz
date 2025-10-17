using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a wave of NPCs, typically for a minigame like the TzHaar Fight Cave.
    /// </summary>
    public record WaveDto
    {
        /// <summary>
        /// A data transfer object representing a set of NPCs to be spawned in a wave.
        /// </summary>
        /// <param name="NpcId">The ID of the NPC to spawn.</param>
        /// <param name="Count">The number of NPCs of this type to spawn.</param>
        public record NpcWaveDto(int NpcId, int Count);

        /// <summary>
        /// Gets the unique identifier for the wave.
        /// </summary>
        public required int WaveId { get; init; }

        /// <summary>
        /// Gets a list of the NPCs that will be spawned in this wave, along with their counts.
        /// </summary>
        public required IReadOnlyList<NpcWaveDto> Npcs { get; init; }
    }
}