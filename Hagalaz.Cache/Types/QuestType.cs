using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represents a quest definition (index 2, archive 35, file = quest id).
    /// </summary>
    public class QuestType : IQuestType
    {
        /// <inheritdoc />
        public int Id { get; }

        /// <inheritdoc />
        public string Name { get; internal set; } = string.Empty;

        /// <inheritdoc />
        public int QuestPoints { get; internal set; }

        /// <inheritdoc />
        public int Category { get; internal set; }

        /// <inheritdoc />
        public int Difficulty { get; internal set; }

        /// <inheritdoc />
        public bool Members { get; internal set; }

        /// <inheritdoc />
        public int GraphicId { get; internal set; } = -1;

        /// <inheritdoc />
        public int QuestPointRequirement { get; internal set; }

        /// <inheritdoc />
        public IReadOnlyList<QuestProgressThreshold> ProgressVarps { get; internal set; } = [];

        /// <inheritdoc />
        public IReadOnlyList<QuestProgressThreshold> ProgressVarBits { get; internal set; } = [];

        /// <inheritdoc />
        public IReadOnlyList<int> PrerequisiteQuestIds { get; internal set; } = [];

        /// <inheritdoc />
        public IReadOnlyList<QuestSkillRequirement> SkillRequirements { get; internal set; } = [];

        /// <inheritdoc />
        public IReadOnlyList<QuestVariableRequirement> VarpRequirements { get; internal set; } = [];

        /// <inheritdoc />
        public IReadOnlyList<QuestVariableRequirement> VarbitRequirements { get; internal set; } = [];

        /// <inheritdoc />
        public IReadOnlyDictionary<int, object> ExtraData { get; internal set; } = new Dictionary<int, object>();

        // Opcodes whose meaning is not yet proven. They are kept only so that decode followed by encode loses nothing;
        // their names are deliberately neutral and they are not part of IQuestType.
        // Evidence: client QuestDefinition.java and all 208 quests of the local revision-742 cache (no other opcode occurs).

        /// <summary>
        /// Opcode 2 string. Proven: the client calls it eventName, falls back to the quest name when absent, and exposes it
        /// through CS2 command 719. Present in 12 of 208 quests: equal to the name in 8, otherwise the literal text
        /// <c>"Name (sort)"</c> including the quotes. None of the 6959 CS2 scripts in the local cache (all decoded, instruction
        /// streams verified) calls command 719 or any other client quest command, so no script consumer proves its purpose
        /// and it stays neutral.
        /// </summary>
        internal string? Opcode2Text { get; set; }

        /// <summary>Opcode 5 unsigned short. The client discards it. Always 30 in the 9 quests that carry it (ids 84, 159-166).</summary>
        internal int? Opcode5Value { get; set; }

        /// <summary>
        /// Opcode 10 int array. The client stores it but nothing ever reads it. Present in 5 quests (77, 158, 179, 205, 206)
        /// with a single value each (about 50 million); the old "interface/component" packing guess does not fit those values.
        /// </summary>
        internal IReadOnlyList<int> Opcode10Values { get; set; } = [];

        /// <summary>Opcode 12 int. The client discards it. It occurs in no quest of the local cache.</summary>
        internal int? Opcode12Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuestType" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public QuestType(int id) => Id = id;
    }
}
