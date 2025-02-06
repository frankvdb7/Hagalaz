using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record WaveDto
    {
        public record NpcWaveDto(int NpcId, int Count);

        /// <summary>
        /// Contains the wave identifier.
        /// </summary>
        public required int WaveId { get; init; }

        /// <summary>
        /// Contains the npcs identifiers, with spawn count.
        /// </summary>
        public required IReadOnlyList<NpcWaveDto> Npcs { get; init; }
    }
}