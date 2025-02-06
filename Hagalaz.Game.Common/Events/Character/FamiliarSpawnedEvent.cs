using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class FamiliarSpawnedEvent : CharacterEvent
    {
        /// <summary>
        /// Contains the familiar.
        /// </summary>
        public INpc Familiar { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FamiliarSpawnedEvent"/> class.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="familiar">The familiar.</param>
        public FamiliarSpawnedEvent(ICharacter character, INpc familiar)
            : base(character) =>
            Familiar = familiar;
    }
}