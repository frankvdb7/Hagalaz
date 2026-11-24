using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Agility
{
    /// <summary>
    ///     Functionality shared by the Agility courses.
    /// </summary>
    public interface IAgilitySkillService
    {
        /// <summary>
        ///     Grants the persisted experience of the obstacle with the given object id. Nothing is granted when the obstacle has no definition.
        /// </summary>
        void AddObstacleExperience(ICharacter character, int objectId);

        /// <summary>
        ///     Gets the Agility level required to attempt the obstacle with the given object id; 1 when the obstacle has no definition.
        /// </summary>
        int GetRequiredLevel(int objectId);

        /// <summary>
        ///     Completes the course, if any, whose obstacles the character has all finished.
        /// </summary>
        void CheckCourseCompletion(ICharacter character);

        /// <summary>
        ///     Makes a visible NPC that is not already speaking say the given text (course trainer encouragement).
        /// </summary>
        void MakeNearbyNpcSpeak(ICharacter character, string npcSpeakText);
    }
}
