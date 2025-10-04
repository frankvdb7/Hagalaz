using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    ///  Represents quest definition.
    /// </summary>
    public class QuestType : IType, IQuestType
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; }
        /// <summary>
        /// Gets the quest points.
        /// </summary>
        /// <value>
        /// The quest points.
        /// </value>
        public int QuestPoints { get; internal set; }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; internal set; }
        /// <summary>
        /// Gets the extra data.
        /// </summary>
        /// <value>
        /// The extra data.
        /// </value>
        public Dictionary<int, object> ExtraData { get; internal set; }
        /// <summary>
        /// Gets the minimum variable bit value.
        /// </summary>
        /// <value>
        /// The minimum variable bit value.
        /// </value>
        public int[] MinVarBitValue { get; internal set; }
        /// <summary>
        /// Gets the progress variable bits.
        /// </summary>
        /// <value>
        /// The progress variable bits.
        /// </value>
        public int[,] ProgressVarBits { get; internal set; }
        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>
        /// The category.
        /// </value>
        public int Category { get; internal set; } = 0;
        /// <summary>
        /// Gets the quest requirements.
        /// </summary>
        /// <value>
        /// The quest requirements.
        /// </value>
        public int[] QuestRequirements { get; internal set; }
        /// <summary>
        /// Gets the name of the sort.
        /// </summary>
        /// <value>
        /// The name of the sort.
        /// </value>
        public string SortName { get; internal set; }
        /// <summary>
        /// Gets an int array4092.
        /// </summary>
        /// <value>
        /// An int array4092.
        /// </value>
        public int[] AnIntArray4092 { get; internal set; }
        /// <summary>
        /// Gets the variable bit requirements.
        /// </summary>
        /// <value>
        /// The variable bit requirements.
        /// </value>
        public int[] VarBitRequirements { get; internal set; }
        /// <summary>
        /// Gets the stat requirements.
        /// </summary>
        /// <value>
        /// The stat requirements.
        /// </value>
        public int[,] StatRequirements { get; internal set; }
        /// <summary>
        /// Gets the quest point requirement.
        /// </summary>
        /// <value>
        /// The quest point requirement.
        /// </value>
        public int QuestPointRequirement { get; internal set; }
        /// <summary>
        /// Gets the varp requirements.
        /// </summary>
        /// <value>
        /// The varp requirements.
        /// </value>
        public int[] VarpRequirements { get; internal set; }
        /// <summary>
        /// Gets the difficulty.
        /// </summary>
        /// <value>
        /// The difficulty.
        /// </value>
        public int Difficulty { get; internal set; } = 0;
        /// <summary>
        /// Gets the maximum varp value.
        /// </summary>
        /// <value>
        /// The maximum varp value.
        /// </value>
        public int[] MaxVarpValue { get; internal set; }
        /// <summary>
        /// Gets the varp requirement names.
        /// </summary>
        /// <value>
        /// The varp requirement names.
        /// </value>
        public string[] VarpRequirementNames { get; internal set; }
        /// <summary>
        /// Gets the progress varps.
        /// </summary>
        /// <value>
        /// The progress varps.
        /// </value>
        public int[,] ProgressVarps { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="QuestType"/> is members.
        /// </summary>
        /// <value>
        ///   <c>true</c> if members; otherwise, <c>false</c>.
        /// </value>
        public bool Members { get; internal set; } = false;
        /// <summary>
        /// Gets the maximum variable bit value.
        /// </summary>
        /// <value>
        /// The maximum variable bit value.
        /// </value>
        public int[] MaxVarBitValue { get; internal set; }
        /// <summary>
        /// Gets the varbit requirement names.
        /// </summary>
        /// <value>
        /// The varbit requirement names.
        /// </value>
        public string[] VarbitRequirementNames { get; internal set; }
        /// <summary>
        /// Gets the minimum varp value.
        /// </summary>
        /// <value>
        /// The minimum varp value.
        /// </value>
        public int[] MinVarpValue { get; internal set; }
        /// <summary>
        /// Gets the graphic identifier.
        /// </summary>
        /// <value>
        /// The graphic identifier.
        /// </value>
        public int GraphicId { get; internal set; } = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuestType" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public QuestType(int id)
        {
            Id = id;
            Name = string.Empty;
            ExtraData = new Dictionary<int, object>();
            MinVarBitValue = Array.Empty<int>();
            ProgressVarBits = new int[0, 0];
            QuestRequirements = Array.Empty<int>();
            SortName = string.Empty;
            AnIntArray4092 = Array.Empty<int>();
            VarBitRequirements = Array.Empty<int>();
            StatRequirements = new int[0, 0];
            VarpRequirements = Array.Empty<int>();
            MaxVarpValue = Array.Empty<int>();
            VarpRequirementNames = Array.Empty<string>();
            ProgressVarps = new int[0, 0];
            MaxVarBitValue = Array.Empty<int>();
            VarbitRequirementNames = Array.Empty<string>();
            MinVarpValue = Array.Empty<int>();
        }

    }
}
