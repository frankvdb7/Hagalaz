using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

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
                    questType.AnIntArray4092[slot] =
                        buffer.ReadInt(); // & 0xFF = componentID | >> 16 = interfaceID
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

    public MemoryStream Encode(IQuestType instance)
    {
        var stream = new MemoryStream();
        var questType = (QuestType)instance;

        if (questType.Name != null)
        {
            stream.WriteByte(1);
            stream.WriteVString(questType.Name);
        }

        if (questType.SortName != null)
        {
            stream.WriteByte(2);
            stream.WriteVString(questType.SortName);
        }

        if (questType.ProgressVarps != null)
        {
            stream.WriteByte(3);
            stream.WriteByte((byte)questType.ProgressVarps.GetLength(0));
            for (int i = 0; i < questType.ProgressVarps.GetLength(0); i++)
            {
                stream.WriteShort(questType.ProgressVarps[i, 0]);
                stream.WriteInt(questType.ProgressVarps[i, 1]);
                stream.WriteInt(questType.ProgressVarps[i, 2]);
            }
        }

        if (questType.ProgressVarBits != null)
        {
            stream.WriteByte(4);
            stream.WriteByte((byte)questType.ProgressVarBits.GetLength(0));
            for (int i = 0; i < questType.ProgressVarBits.GetLength(0); i++)
            {
                stream.WriteShort(questType.ProgressVarBits[i, 0]);
                stream.WriteInt(questType.ProgressVarBits[i, 1]);
                stream.WriteInt(questType.ProgressVarBits[i, 2]);
            }
        }

        if (questType.Category != 0)
        {
            stream.WriteByte(6);
            stream.WriteByte((byte)questType.Category);
        }

        if (questType.Difficulty != 0)
        {
            stream.WriteByte(7);
            stream.WriteByte((byte)questType.Difficulty);
        }

        if (questType.Members)
        {
            stream.WriteByte(8);
        }

        if (questType.QuestPoints > 0)
        {
            stream.WriteByte(9);
            stream.WriteByte((byte)questType.QuestPoints);
        }

        if (questType.QuestRequirements != null)
        {
            stream.WriteByte(13);
            stream.WriteByte((byte)questType.QuestRequirements.Length);
            foreach (var req in questType.QuestRequirements)
            {
                stream.WriteShort(req);
            }
        }

        if (questType.StatRequirements != null)
        {
            stream.WriteByte(14);
            stream.WriteByte((byte)questType.StatRequirements.GetLength(0));
            for (int i = 0; i < questType.StatRequirements.GetLength(0); i++)
            {
                stream.WriteByte((byte)questType.StatRequirements[i, 0]);
                stream.WriteByte((byte)questType.StatRequirements[i, 1]);
            }
        }

        if (questType.QuestPointRequirement > 0)
        {
            stream.WriteByte(15);
            stream.WriteShort(questType.QuestPointRequirement);
        }

        if (questType.GraphicId != -1)
        {
            stream.WriteByte(17);
            stream.WriteBigSmart(questType.GraphicId);
        }

        if (questType.VarpRequirements != null)
        {
            stream.WriteByte(18);
            stream.WriteByte((byte)questType.VarpRequirements.Length);
            for (int i = 0; i < questType.VarpRequirements.Length; i++)
            {
                stream.WriteInt(questType.VarpRequirements[i]);
                stream.WriteInt(questType.MinVarpValue[i]);
                stream.WriteInt(questType.MaxVarpValue[i]);
                stream.WriteString(questType.VarpRequirementNames[i]);
            }
        }

        if (questType.VarBitRequirements != null)
        {
            stream.WriteByte(19);
            stream.WriteByte((byte)questType.VarBitRequirements.Length);
            for (int i = 0; i < questType.VarBitRequirements.Length; i++)
            {
                stream.WriteInt(questType.VarBitRequirements[i]);
                stream.WriteInt(questType.MinVarBitValue[i]);
                stream.WriteInt(questType.MaxVarBitValue[i]);
                stream.WriteString(questType.VarbitRequirementNames[i]);
            }
        }

        if (questType.ExtraData?.Count > 0)
        {
            stream.WriteByte(249);
            stream.WriteByte((byte)questType.ExtraData.Count);
            foreach (var (key, value) in questType.ExtraData)
            {
                stream.WriteByte((byte)(value is string ? 1 : 0));
                stream.WriteMedInt(key);
                if (value is string s)
                {
                    stream.WriteString(s);
                }
                else
                {
                    stream.WriteInt((int)value);
                }
            }
        }


        stream.WriteByte(0); // End of data
        return stream;
    }
}