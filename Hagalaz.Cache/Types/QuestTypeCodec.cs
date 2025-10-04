using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    public class QuestTypeCodec : IQuestTypeCodec
    {
        public IQuestType Decode(int id, MemoryStream stream)
        {
            var questType = new QuestType(id);
            Decode(questType, stream);
            return questType;
        }

        private void Decode(QuestType questType, MemoryStream buffer)
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
                    questType.Name = buffer.ReadVString();
                }
                else if (opcode == 2)
                {
                    questType.SortName = buffer.ReadVString();
                }
                else if (3 == opcode)
                {
                    int count = buffer.ReadUnsignedByte();
                    questType.ProgressVarps = new int[count, 3];
                    for (int slot = 0; slot < count; slot++)
                    {
                        questType.ProgressVarps[slot, 0] = buffer.ReadUnsignedShort();
                        questType.ProgressVarps[slot, 1] = buffer.ReadInt();
                        questType.ProgressVarps[slot, 2] = buffer.ReadInt();
                    }
                }
                else if (opcode == 4)
                {
                    int count = buffer.ReadUnsignedByte();
                    questType.ProgressVarBits = new int[count, 3];
                    for (int slot = 0; slot < count; slot++)
                    {
                        questType.ProgressVarBits[slot, 0] = buffer.ReadUnsignedShort();
                        questType.ProgressVarBits[slot, 1] = buffer.ReadInt();
                        questType.ProgressVarBits[slot, 2] = buffer.ReadInt();
                    }
                }
                else if (5 == opcode)
                {
                    buffer.ReadUnsignedShort();
                }
                else if (opcode == 6)
                {
                    questType.Category = buffer.ReadUnsignedByte();
                }
                else if (7 == opcode)
                {
                    questType.Difficulty = buffer.ReadUnsignedByte();
                }
                else if (opcode == 8)
                {
                    questType.Members = true;
                }
                else if (opcode == 9)
                {
                    questType.QuestPoints = buffer.ReadUnsignedByte();
                }
                else if (opcode == 10)
                {
                    // not used
                    int count = buffer.ReadUnsignedByte();
                    questType.AnIntArray4092 = new int[count];
                    for (int slot = 0; slot < count; slot++)
                    {
                        questType.AnIntArray4092[slot] = buffer.ReadInt(); // & 0xFF = componentID | >> 16 = interfaceID
                    }
                }
                else if (opcode == 12)
                {
                    buffer.ReadInt();
                }
                else if (opcode == 13)
                {
                    int count = buffer.ReadUnsignedByte();
                    questType.QuestRequirements = new int[count];
                    for (int slot = 0; slot < count; slot++)
                    {
                        questType.QuestRequirements[slot] = buffer.ReadUnsignedShort();
                    }
                }
                else if (opcode == 14)
                {
                    int count = buffer.ReadUnsignedByte();
                    questType.StatRequirements = new int[count, 2];
                    for (int slot = 0; slot < count; slot++)
                    {
                        questType.StatRequirements[slot, 0] = buffer.ReadUnsignedByte();
                        questType.StatRequirements[slot, 1] = buffer.ReadUnsignedByte();
                    }
                }
                else if (opcode == 15)
                {
                    questType.QuestPointRequirement = buffer.ReadUnsignedShort();
                }
                else if (17 == opcode)
                {
                    questType.GraphicId = buffer.ReadBigSmart(); // sprite
                }
                else if (opcode == 18)
                {
                    int count = buffer.ReadUnsignedByte();
                    questType.VarpRequirements = new int[count];
                    questType.MinVarpValue = new int[count];
                    questType.MaxVarpValue = new int[count];
                    questType.VarpRequirementNames = new string[count];
                    for (int i32 = 0; i32 < count; i32++)
                    {
                        questType.VarpRequirements[i32] = buffer.ReadInt();
                        questType.MinVarpValue[i32] = buffer.ReadInt();
                        questType.MaxVarpValue[i32] = buffer.ReadInt();
                        questType.VarpRequirementNames[i32] = buffer.ReadString();
                    }
                }
                else if (19 == opcode)
                {
                    int count = buffer.ReadUnsignedByte();
                    questType.VarBitRequirements = new int[count];
                    questType.MinVarBitValue = new int[count];
                    questType.MaxVarBitValue = new int[count];
                    questType.VarbitRequirementNames = new string[count];
                    for (int i34 = 0; i34 < count; i34++)
                    {
                        questType.VarBitRequirements[i34] = buffer.ReadInt();
                        questType.MinVarBitValue[i34] = buffer.ReadInt();
                        questType.MaxVarBitValue[i34] = buffer.ReadInt();
                        questType.VarbitRequirementNames[i34] = buffer.ReadString();
                    }
                }
                else if (opcode == 249)
                {
                    int size = buffer.ReadUnsignedByte();
                    questType.ExtraData = new Dictionary<int, object>(size);
                    for (int i37 = 0; i37 < size; i37++)
                    {
                        bool stringInstance = buffer.ReadUnsignedByte() == 1;
                        int key = buffer.ReadMedInt();
                        object val;
                        if (stringInstance)
                            val = buffer.ReadString();
                        else
                            val = buffer.ReadInt();
                        if (questType.ExtraData.ContainsKey(key))
                            questType.ExtraData.Remove(key);
                        questType.ExtraData.Add(key, val);
                    }
                }
            }
        }

        public MemoryStream Encode(IQuestType instance) => throw new NotImplementedException();
    }
}