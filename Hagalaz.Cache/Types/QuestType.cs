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
        public int QuestPoints { get; private set; }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }
        /// <summary>
        /// Gets the extra data.
        /// </summary>
        /// <value>
        /// The extra data.
        /// </value>
        public Dictionary<int, object> ExtraData { get; private set; }
        /// <summary>
        /// Gets the minimum variable bit value.
        /// </summary>
        /// <value>
        /// The minimum variable bit value.
        /// </value>
        public int[] MinVarBitValue { get; private set; }
        /// <summary>
        /// Gets the progress variable bits.
        /// </summary>
        /// <value>
        /// The progress variable bits.
        /// </value>
        public int[,] ProgressVarBits { get; private set; }
        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>
        /// The category.
        /// </value>
        public int Category { get; private set; } = 0;
        /// <summary>
        /// Gets the quest requirements.
        /// </summary>
        /// <value>
        /// The quest requirements.
        /// </value>
        public int[] QuestRequirements { get; private set; }
        /// <summary>
        /// Gets the name of the sort.
        /// </summary>
        /// <value>
        /// The name of the sort.
        /// </value>
        public string SortName { get; private set; }
        /// <summary>
        /// Gets an int array4092.
        /// </summary>
        /// <value>
        /// An int array4092.
        /// </value>
        public int[] AnIntArray4092 { get; private set; }
        /// <summary>
        /// Gets the variable bit requirements.
        /// </summary>
        /// <value>
        /// The variable bit requirements.
        /// </value>
        public int[] VarBitRequirements { get; private set; }
        /// <summary>
        /// Gets the stat requirements.
        /// </summary>
        /// <value>
        /// The stat requirements.
        /// </value>
        public int[,] StatRequirements { get; private set; }
        /// <summary>
        /// Gets the quest point requirement.
        /// </summary>
        /// <value>
        /// The quest point requirement.
        /// </value>
        public int QuestPointRequirement { get; private set; }
        /// <summary>
        /// Gets the varp requirements.
        /// </summary>
        /// <value>
        /// The varp requirements.
        /// </value>
        public int[] VarpRequirements { get; private set; }
        /// <summary>
        /// Gets the difficulty.
        /// </summary>
        /// <value>
        /// The difficulty.
        /// </value>
        public int Difficulty { get; private set; } = 0;
        /// <summary>
        /// Gets the maximum varp value.
        /// </summary>
        /// <value>
        /// The maximum varp value.
        /// </value>
        public int[] MaxVarpValue { get; private set; }
        /// <summary>
        /// Gets the varp requirement names.
        /// </summary>
        /// <value>
        /// The varp requirement names.
        /// </value>
        public string[] VarpRequirementNames { get; private set; }
        /// <summary>
        /// Gets the progress varps.
        /// </summary>
        /// <value>
        /// The progress varps.
        /// </value>
        public int[,] ProgressVarps { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="QuestType"/> is members.
        /// </summary>
        /// <value>
        ///   <c>true</c> if members; otherwise, <c>false</c>.
        /// </value>
        public bool Members { get; private set; } = false;
        /// <summary>
        /// Gets the maximum variable bit value.
        /// </summary>
        /// <value>
        /// The maximum variable bit value.
        /// </value>
        public int[] MaxVarBitValue { get; private set; }
        /// <summary>
        /// Gets the varbit requirement names.
        /// </summary>
        /// <value>
        /// The varbit requirement names.
        /// </value>
        public string[] VarbitRequirementNames { get; private set; }
        /// <summary>
        /// Gets the minimum varp value.
        /// </summary>
        /// <value>
        /// The minimum varp value.
        /// </value>
        public int[] MinVarpValue { get; private set; }
        /// <summary>
        /// Gets the graphic identifier.
        /// </summary>
        /// <value>
        /// The graphic identifier.
        /// </value>
        public int GraphicId { get; private set; } = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuestType" /> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public QuestType(int id)
        {
            Id = id;
        }

        /// <summary>
        /// Parses the opcode.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        public void Decode(MemoryStream buffer)
        {
            while (true)
            {
                int opcode = buffer.ReadUnsignedByte();
                if (opcode == 0)
                {
                    return;
                }
                if (opcode == 1)
                {
                    Name = buffer.ReadVString();
                }
                else if (opcode == 2)
                {
                    SortName = buffer.ReadVString();
                }
                else if (3 == opcode)
                {
                    int count = buffer.ReadUnsignedByte();
                    ProgressVarps = new int[count, 3];
                    for (int slot = 0; slot < count; slot++)
                    {
                        ProgressVarps[slot, 0] = buffer.ReadUnsignedShort();
                        ProgressVarps[slot, 1] = buffer.ReadInt();
                        ProgressVarps[slot, 2] = buffer.ReadInt();
                    }
                }
                else if (opcode == 4)
                {
                    int count = buffer.ReadUnsignedByte();
                    ProgressVarBits = new int[count, 3];
                    for (int slot = 0; slot < count; slot++)
                    {
                        ProgressVarBits[slot, 0] = buffer.ReadUnsignedShort();
                        ProgressVarBits[slot, 1] = buffer.ReadInt();
                        ProgressVarBits[slot, 2] = buffer.ReadInt();
                    }
                }
                else if (5 == opcode)
                {
                    buffer.ReadUnsignedShort();
                }
                else if (opcode == 6)
                {
                    Category = buffer.ReadUnsignedByte();
                }
                else if (7 == opcode)
                {
                    Difficulty = buffer.ReadUnsignedByte();
                }
                else if (opcode == 8)
                {
                    Members = true;
                }
                else if (opcode == 9)
                {
                    QuestPoints = buffer.ReadUnsignedByte();
                }
                else if (opcode == 10)
                {
                    // not used
                    int count = buffer.ReadUnsignedByte();
                    AnIntArray4092 = new int[count];
                    for (int slot = 0; slot < count; slot++)
                    {
                        AnIntArray4092[slot] = buffer.ReadInt(); // & 0xFF = componentID | >> 16 = interfaceID
                    }
                }
                else if (opcode == 12)
                {
                    buffer.ReadInt();
                }
                else if (opcode == 13)
                {
                    int count = buffer.ReadUnsignedByte();
                    QuestRequirements = new int[count];
                    for (int slot = 0; slot < count; slot++)
                    {
                        QuestRequirements[slot] = buffer.ReadUnsignedShort();
                    }
                }
                else if (opcode == 14)
                {
                    int count = buffer.ReadUnsignedByte();
                    StatRequirements = new int[count, 2];
                    for (int slot = 0; slot < count; slot++)
                    {
                        StatRequirements[slot, 0] = buffer.ReadUnsignedByte();
                        StatRequirements[slot, 1] = buffer.ReadUnsignedByte();
                    }
                }
                else if (opcode == 15)
                {
                    QuestPointRequirement = buffer.ReadUnsignedShort();
                }
                else if (17 == opcode)
                {
                    GraphicId = buffer.ReadBigSmart(); // sprite
                }
                else if (opcode == 18)
                {
                    int count = buffer.ReadUnsignedByte();
                    VarpRequirements = new int[count];
                    MinVarpValue = new int[count];
                    MaxVarpValue = new int[count];
                    VarpRequirementNames = new string[count];
                    for (int i32 = 0; i32 < count; i32++)
                    {
                        VarpRequirements[i32] = buffer.ReadInt();
                        MinVarpValue[i32] = buffer.ReadInt();
                        MaxVarpValue[i32] = buffer.ReadInt();
                        VarpRequirementNames[i32] = buffer.ReadString();
                    }
                }
                else if (19 == opcode)
                {
                    int count = buffer.ReadUnsignedByte();
                    VarBitRequirements = new int[count];
                    MinVarBitValue = new int[count];
                    MaxVarBitValue = new int[count];
                    VarbitRequirementNames = new string[count];
                    for (int i34 = 0; i34 < count; i34++)
                    {
                        VarBitRequirements[i34] = buffer.ReadInt();
                        MinVarBitValue[i34] = buffer.ReadInt();
                        MaxVarBitValue[i34] = buffer.ReadInt();
                        VarbitRequirementNames[i34] = buffer.ReadString();
                    }
                }
                else if (opcode == 249)
                {
                    int size = buffer.ReadUnsignedByte();
                    ExtraData = new Dictionary<int, object>(size);
                    for (int i37 = 0; i37 < size; i37++)
                    {
                        bool stringInstance = buffer.ReadUnsignedByte() == 1;
                        int key = buffer.ReadMedInt();
                        object val;
                        if (stringInstance)
                            val = buffer.ReadString();
                        else
                            val = buffer.ReadInt();
                        if (ExtraData.ContainsKey(key))
                            ExtraData.Remove(key);
                        ExtraData.Add(key, val);
                    }
                }
            }
        }

        /// <summary>
        /// Encodes this instance.
        /// </summary>
        /// <returns></returns>
        public MemoryStream Encode() => null;
    }
}
