using System.Collections.Generic;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines the read-only static quest definition stored in the game cache (index 2, archive 35, file = quest id).
    /// </summary>
    /// <remarks>
    /// Only metadata whose meaning has been verified against the client is exposed. Opcodes that are still
    /// unverified (2, 5, 10, 12) are intentionally not part of this contract. Player quest state is not cache data.
    /// </remarks>
    public interface IQuestType : IType
    {
        /// <summary>Gets the display name.</summary>
        string Name { get; }

        /// <summary>Gets the quest points awarded on completion.</summary>
        int QuestPoints { get; }

        /// <summary>Gets the category id.</summary>
        int Category { get; }

        /// <summary>Gets the difficulty id.</summary>
        int Difficulty { get; }

        /// <summary>Gets a value indicating whether the quest is members-only.</summary>
        bool Members { get; }

        /// <summary>Gets the graphic (sprite) id, or -1 when the definition has none.</summary>
        int GraphicId { get; }

        /// <summary>Gets the total quest points required to start the quest.</summary>
        int QuestPointRequirement { get; }

        /// <summary>Gets the varp thresholds that mark the quest as started and completed.</summary>
        IReadOnlyList<QuestProgressThreshold> ProgressVarps { get; }

        /// <summary>Gets the varbit thresholds that mark the quest as started and completed.</summary>
        IReadOnlyList<QuestProgressThreshold> ProgressVarBits { get; }

        /// <summary>Gets the ids of the quests that must be completed first.</summary>
        IReadOnlyList<int> PrerequisiteQuestIds { get; }

        /// <summary>Gets the minimum skill levels required.</summary>
        IReadOnlyList<QuestSkillRequirement> SkillRequirements { get; }

        /// <summary>Gets the varp requirements.</summary>
        IReadOnlyList<QuestVariableRequirement> VarpRequirements { get; }

        /// <summary>Gets the varbit requirements.</summary>
        IReadOnlyList<QuestVariableRequirement> VarbitRequirements { get; }

        /// <summary>Gets the definition parameter map (opcode 249).</summary>
        IReadOnlyDictionary<int, object> ExtraData { get; }
    }
}
