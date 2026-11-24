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
        ///     Completes the basic or advanced Gnome course when the character has finished all of its obstacles.
        /// </summary>
        void CheckGnomeCourseCompletion(ICharacter character);

        /// <summary>
        ///     Makes a nearby NPC speak the given text while the character is on the Gnome course.
        /// </summary>
        void CheckGnomeCourseNpCs(ICharacter character, string npcSpeakText);
    }
}
