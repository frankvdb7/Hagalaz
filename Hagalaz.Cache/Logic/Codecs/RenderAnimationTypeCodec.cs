using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>
/// Revision-742 render animation sets, config archive 32. Encodes opcodes in ascending order; a repeated opcode keeps its last
/// value, as the client does.
/// </summary>
public sealed class RenderAnimationTypeCodec : ITypeCodec<IRenderAnimationType>
{
    private static bool IsSequence(int opcode) => opcode is >= 2 and <= 9 or >= 38 and <= 42 or >= 46 and <= 51;

    /// <summary>Payload length of the opcodes kept raw; opcode 28 is counted and handled separately.</summary>
    private static int? RawLength(int opcode) => opcode switch
    {
        29 or 31 or 34 or 37 => 1,
        26 or 30 or 32 or 33 or 35 or 36 or 43 or 44 or 45 or 54 => 2,
        55 => 3,
        56 => 7,
        27 => 13,
        _ => null,
    };

    public IRenderAnimationType Decode(int id, MemoryStream stream)
    {
        var type = new RenderAnimationType(id);
        var reader = new DefinitionReader(stream, "render animation set");
        for (var opcode = reader.Byte(); opcode != 0; opcode = reader.Byte())
        {
            if (opcode == 1) type.StandWalk = new(reader.BigSmart(), reader.BigSmart());
            else if (IsSequence(opcode)) type.Sequences[opcode] = reader.BigSmart();
            else if (opcode == 28) type.RawOpcodes[opcode] = reader.Bytes(reader.Byte());
            else if (RawLength(opcode) is { } length) type.RawOpcodes[opcode] = reader.Bytes(length);
            else if (opcode == 52)
            {
                var sequences = new List<RenderAnimationWeightedSequence>();
                for (var count = reader.Byte(); count > 0; count--) sequences.Add(new(reader.BigSmart(), reader.Byte()));
                type.RandomSequences = sequences;
            }
            else if (opcode == 53) type.Opcode53 = true;
            else throw new NotSupportedException($"Render animation opcode {opcode} is not supported.");
        }
        return type;
    }

    public MemoryStream Encode(IRenderAnimationType type)
    {
        var stream = new MemoryStream();
        var opcodes = type.Sequences.Keys.Concat(type.RawOpcodes.Keys).ToList();
        if (type.StandWalk is not null) opcodes.Add(1);
        if (type.RandomSequences is not null) opcodes.Add(52);
        if (type.Opcode53) opcodes.Add(53);
        foreach (var opcode in opcodes.Order())
        {
            stream.Byte(opcode);
            if (opcode == 1)
            {
                stream.BigSmart(type.StandWalk!.Value.Stand);
                stream.BigSmart(type.StandWalk.Value.Walk);
            }
            else if (opcode == 52)
            {
                stream.Byte(type.RandomSequences!.Count);
                foreach (var random in type.RandomSequences) { stream.BigSmart(random.Sequence); stream.Byte(random.Weight); }
            }
            else if (opcode == 53) { }
            else if (type.Sequences.TryGetValue(opcode, out var sequence)) stream.BigSmart(sequence);
            else
            {
                var payload = type.RawOpcodes[opcode];
                if (opcode == 28) stream.Byte(payload.Length);
                else if (RawLength(opcode) != payload.Length) throw new InvalidOperationException($"Render animation opcode {opcode} needs {RawLength(opcode)} bytes.");
                stream.Write(payload);
            }
        }
        stream.Byte(0);
        return stream;
    }
}
