using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Agility
{
    /// <summary>
    ///     An Agility course: knows which obstacle progress completes it. Rewards are persisted definitions resolved by <see cref="IAgilitySkillService" />.
    /// </summary>
    public interface IAgilityCourse
    {
        /// <summary>
        ///     The identity of the course, matching its persisted definition.
        /// </summary>
        AgilityCourseId Id { get; }

        /// <summary>
        ///     Determines whether the character has finished every obstacle of the course.
        /// </summary>
        bool IsCompleted(ICharacter character);

        /// <summary>
        ///     Clears the character's obstacle progress for the course.
        /// </summary>
        void ClearProgress(ICharacter character);
    }
}
