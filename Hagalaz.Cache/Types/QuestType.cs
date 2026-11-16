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

        /// <summary>Opcode 2 string (the client calls it eventName, earlier Hagalaz code called it SortName; unverified).</summary>
        internal string? Opcode2Text { get; set; }

        /// <summary>Opcode 5 unsigned short payload (read and discarded by the client).</summary>
        internal int? Opcode5Value { get; set; }

        /// <summary>Opcode 10 int array (looks like packed interface/component references; unverified).</summary>
        internal IReadOnlyList<int> Opcode10Values { get; set; } = [];

        /// <summary>Opcode 12 int payload (read and discarded by the client).</summary>
        internal int? Opcode12Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuestType" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public QuestType(int id) => Id = id;
    }
}
