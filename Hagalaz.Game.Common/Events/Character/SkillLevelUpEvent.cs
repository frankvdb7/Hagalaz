using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class SkillLevelUpEvent : CharacterEvent
    {
        /// <summary>
        /// Gets the skill identifier.
        /// </summary>
        public int SkillID { get; }

        /// <summary>
        /// Gets the last skill level.
        /// </summary>
        public int PreviousSkillLevel { get; }

        /// <summary>
        /// Gets the current skill level.
        /// </summary>
        public int CurrentSkillLevel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillLevelUpEvent" /> class.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="skillID">The skill identifier.</param>
        /// <param name="previousSkillLevel">The last skill level.</param>
        /// <param name="currentSkillLevel">The current skill level.</param>
        public SkillLevelUpEvent(ICharacter character, int skillID, int previousSkillLevel, int currentSkillLevel) :
            base(character)
        {
            SkillID = skillID;
            PreviousSkillLevel = previousSkillLevel;
            CurrentSkillLevel = currentSkillLevel;
        }
    }
}