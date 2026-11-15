using System;
using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Revision-742 animation skeletons, cache index 1: slot count, then types, enabled bytes, masks, group lengths and groups.</summary>
public sealed class AnimationSkeletonTypeCodec : ITypeCodec<IAnimationSkeletonType>
{
    public IAnimationSkeletonType Decode(int id, MemoryStream stream)
    {
        var reader = new DefinitionReader(stream, "animation skeleton");
        var count = reader.UShort();
        var skeleton = new AnimationSkeletonType(id)
        {
            Types = new int[count], Enabled = new int[count], Masks = new int[count], Groups = new int[count][],
        };
        for (var slot = 0; slot < count; slot++) skeleton.Types[slot] = reader.Byte();
        for (var slot = 0; slot < count; slot++) skeleton.Enabled[slot] = reader.Byte();
        for (var slot = 0; slot < count; slot++) skeleton.Masks[slot] = reader.UShort();
        for (var slot = 0; slot < count; slot++) skeleton.Groups[slot] = new int[reader.UnsignedSmart()];
        foreach (var group in skeleton.Groups)
            for (var entry = 0; entry < group.Length; entry++) group[entry] = reader.UnsignedSmart();
        reader.End();
        return skeleton;
    }

    public MemoryStream Encode(IAnimationSkeletonType skeleton)
    {
        var count = skeleton.Types.Length;
        if (skeleton.Enabled.Length != count || skeleton.Masks.Length != count || skeleton.Groups.Length != count)
            throw new InvalidOperationException("Every skeleton slot needs a type, an enabled byte, a mask and a group.");
        var stream = new MemoryStream();
        stream.UShort(count);
        foreach (var type in skeleton.Types) stream.Byte(type);
        foreach (var enabled in skeleton.Enabled) stream.Byte(enabled);
        foreach (var mask in skeleton.Masks) stream.UShort(mask);
        foreach (var group in skeleton.Groups) stream.UnsignedSmart(group.Length);
        foreach (var group in skeleton.Groups)
            foreach (var entry in group) stream.UnsignedSmart(entry);
        return stream;
    }
}
