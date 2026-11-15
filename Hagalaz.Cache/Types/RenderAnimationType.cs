using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types;

public sealed class RenderAnimationType(int id) : IRenderAnimationType
{
    private const int RandomSequencesOpcode = 52;

    public int Id { get; } = id;
    public RenderAnimationSequencePair? StandWalk { get; set; }
    public IDictionary<int, int> Sequences { get; } = new SortedDictionary<int, int>();
    public IList<RenderAnimationWeightedSequence>? RandomSequences { get; set; }
    public bool Opcode53 { get; set; }
    public IDictionary<int, byte[]> RawOpcodes { get; } = new SortedDictionary<int, byte[]>();

    public int[] SequenceIds
    {
        get
        {
            var ids = new List<int>();
            if (StandWalk is { } standWalk) ids.AddRange([standWalk.Stand, standWalk.Walk]);
            foreach (var opcode in Sequences.Keys.Append(RandomSequencesOpcode).Order())
            {
                if (opcode == RandomSequencesOpcode) ids.AddRange(RandomSequences?.Select(random => random.Sequence) ?? []);
                else ids.Add(Sequences[opcode]);
            }
            return ids.Where(id => id >= 0).Distinct().ToArray();
        }
    }
}
