using System.Collections.Generic;

namespace Hagalaz.Cache.Abstractions.Types;

/// <summary>
/// A render animation set (config index 2, archive 32). Values are as stored; opcodes whose meaning is unknown are kept as raw
/// payloads keyed by opcode.
/// </summary>
public interface IRenderAnimationType : IType
{
    /// <summary>Opcode 1: standing and walking sequences.</summary>
    RenderAnimationSequencePair? StandWalk { get; set; }
    /// <summary>Opcodes 2-9, 38-42 and 46-51: one sequence each, keyed by opcode.</summary>
    IDictionary<int, int> Sequences { get; }
    /// <summary>Opcode 52: weighted random sequences.</summary>
    IList<RenderAnimationWeightedSequence>? RandomSequences { get; set; }
    bool Opcode53 { get; set; }
    /// <summary>
    /// Opcodes 26-37, 43-45 and 54-56: payloads keyed by opcode. Opcode 28's payload excludes its leading count byte.
    /// </summary>
    IDictionary<int, byte[]> RawOpcodes { get; }

    /// <summary>
    /// The sequences the set refers to: opcode 1's first, then the others in ascending opcode order, distinct, negatives dropped.
    /// </summary>
    int[] SequenceIds { get; }
}

public readonly record struct RenderAnimationSequencePair(int Stand, int Walk);

public readonly record struct RenderAnimationWeightedSequence(int Sequence, int Weight);
