using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>
/// Revision-742 quest definitions, index 2 archive 35.
/// </summary>
/// <remarks>
/// Decode fails on opcodes the client does not define: skipping one would desynchronise the rest of the definition.
/// Encode is a semantic inverse of decode, including the opcodes whose meaning is still unproven (2, 5, 10, 12),
/// so decoding, mutating verified fields and encoding never drops cache data. Encode emits opcodes in ascending
/// order and omits empty or default fields, so it does not reproduce a source definition byte for byte.
/// </remarks>
public class QuestTypeCodec : IQuestTypeCodec
{
    public IQuestType Decode(int id, MemoryStream stream)
    {
        var questType = new QuestType(id);
        while (true)
        {
            int opcode = stream.ReadUnsignedByte();
            if (opcode == 0)
            {
                return questType;
            }

            switch (opcode)
            {
                case 1: questType.Name = stream.ReadVString(); break;
                case 2: questType.Opcode2Text = stream.ReadVString(); break;
                case 3: questType.ProgressVarps = ReadProgress(stream); break;
                case 4: questType.ProgressVarBits = ReadProgress(stream); break;
                case 5: questType.Opcode5Value = stream.ReadUnsignedShort(); break;
                case 6: questType.Category = stream.ReadUnsignedByte(); break;
                case 7: questType.Difficulty = stream.ReadUnsignedByte(); break;
                case 8: questType.Members = true; break;
                case 9: questType.QuestPoints = stream.ReadUnsignedByte(); break;
                case 10: questType.Opcode10Values = ReadList(stream, static s => s.ReadInt()); break;
                case 12: questType.Opcode12Value = stream.ReadInt(); break;
                case 13: questType.PrerequisiteQuestIds = ReadList(stream, static s => s.ReadUnsignedShort()); break;
                case 14: questType.SkillRequirements = ReadList(stream, static s => new QuestSkillRequirement(s.ReadUnsignedByte(), s.ReadUnsignedByte())); break;
                case 15: questType.QuestPointRequirement = stream.ReadUnsignedShort(); break;
                case 17: questType.GraphicId = stream.ReadBigSmart(); break;
                case 18: questType.VarpRequirements = ReadVariableRequirements(stream); break;
                case 19: questType.VarbitRequirements = ReadVariableRequirements(stream); break;
                case 249: questType.ExtraData = ReadParams(stream); break;
                default: throw new InvalidDataException($"Unknown quest opcode {opcode}.");
            }
        }
    }

    public MemoryStream Encode(IQuestType instance)
    {
        var questType = (QuestType)instance;
        var stream = new MemoryStream();

        if (questType.Name.Length > 0)
        {
            stream.WriteByte(1);
            stream.WriteVString(questType.Name);
        }

        if (questType.Opcode2Text is { } opcode2Text)
        {
            stream.WriteByte(2);
            stream.WriteVString(opcode2Text);
        }

        WriteProgress(stream, 3, questType.ProgressVarps);
        WriteProgress(stream, 4, questType.ProgressVarBits);

        if (questType.Opcode5Value is { } opcode5Value)
        {
            stream.WriteByte(5);
            stream.WriteShort(opcode5Value);
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

        if (questType.QuestPoints != 0)
        {
            stream.WriteByte(9);
            stream.WriteByte((byte)questType.QuestPoints);
        }

        WriteList(stream, 10, questType.Opcode10Values, static (s, value) => s.WriteInt(value));

        if (questType.Opcode12Value is { } opcode12Value)
        {
            stream.WriteByte(12);
            stream.WriteInt(opcode12Value);
        }

        WriteList(stream, 13, questType.PrerequisiteQuestIds, static (s, value) => s.WriteShort(value));
        WriteList(stream, 14, questType.SkillRequirements, static (s, requirement) =>
        {
            s.WriteByte((byte)requirement.SkillId);
            s.WriteByte((byte)requirement.Level);
        });

        if (questType.QuestPointRequirement != 0)
        {
            stream.WriteByte(15);
            stream.WriteShort(questType.QuestPointRequirement);
        }

        if (questType.GraphicId != -1)
        {
            stream.WriteByte(17);
            stream.WriteBigSmart(questType.GraphicId);
        }

        WriteVariableRequirements(stream, 18, questType.VarpRequirements);
        WriteVariableRequirements(stream, 19, questType.VarbitRequirements);

        if (questType.ExtraData.Count > 0)
        {
            stream.WriteByte(249);
            WriteCount(stream, questType.ExtraData.Count);
            foreach (var (key, value) in questType.ExtraData)
            {
                stream.WriteByte((byte)(value is string ? 1 : 0));
                stream.WriteMedInt(key);
                if (value is string text)
                {
                    stream.WriteString(text);
                }
                else
                {
                    stream.WriteInt((int)value);
                }
            }
        }

        stream.WriteByte(0);
        return stream;
    }

    private static IReadOnlyList<QuestProgressThreshold> ReadProgress(MemoryStream stream) =>
        ReadList(stream, static s => new QuestProgressThreshold(s.ReadUnsignedShort(), s.ReadInt(), s.ReadInt()));

    private static IReadOnlyList<QuestVariableRequirement> ReadVariableRequirements(MemoryStream stream) =>
        ReadList(stream, static s => new QuestVariableRequirement(s.ReadInt(), s.ReadInt(), s.ReadInt(), s.ReadString()));

    private static IReadOnlyList<T> ReadList<T>(MemoryStream stream, Func<MemoryStream, T> readItem)
    {
        var items = new T[stream.ReadUnsignedByte()];
        for (var index = 0; index < items.Length; index++)
        {
            items[index] = readItem(stream);
        }

        return items;
    }

    private static IReadOnlyDictionary<int, object> ReadParams(MemoryStream stream)
    {
        var values = new Dictionary<int, object>();
        var count = stream.ReadUnsignedByte();
        for (var index = 0; index < count; index++)
        {
            var isString = stream.ReadUnsignedByte() == 1;
            var key = stream.ReadMedInt();
            values[key] = isString ? stream.ReadString() : stream.ReadInt();
        }

        return values;
    }

    private static void WriteProgress(MemoryStream stream, int opcode, IReadOnlyList<QuestProgressThreshold> rows) =>
        WriteList(stream, opcode, rows, static (s, row) =>
        {
            s.WriteShort(row.Id);
            s.WriteInt(row.StartedThreshold);
            s.WriteInt(row.CompletedThreshold);
        });

    private static void WriteVariableRequirements(MemoryStream stream, int opcode, IReadOnlyList<QuestVariableRequirement> rows) =>
        WriteList(stream, opcode, rows, static (s, row) =>
        {
            s.WriteInt(row.Id);
            s.WriteInt(row.Minimum);
            s.WriteInt(row.Maximum);
            s.WriteString(row.Description);
        });

    private static void WriteList<T>(MemoryStream stream, int opcode, IReadOnlyList<T> items, Action<MemoryStream, T> writeItem)
    {
        if (items.Count == 0)
        {
            return;
        }

        stream.WriteByte((byte)opcode);
        WriteCount(stream, items.Count);
        foreach (var item in items)
        {
            writeItem(stream, item);
        }
    }

    private static void WriteCount(MemoryStream stream, int count)
    {
        if (count > byte.MaxValue)
        {
            throw new InvalidOperationException($"A quest definition list holds at most {byte.MaxValue} entries, got {count}.");
        }

        stream.WriteByte((byte)count);
    }
}
