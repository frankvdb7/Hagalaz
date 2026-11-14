using System;
using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types;

public sealed class MaterialTableType(int id) : IMaterialTableType
{
    public int Id { get; } = id;
    public byte[] PresenceFlags { get; set; } = [];
    public IList<MaterialEntry> Materials { get; } = new List<MaterialEntry>();

    public MaterialEntry? Find(int textureId)
    {
        if (textureId < 0 || textureId >= PresenceFlags.Length || PresenceFlags[textureId] != 1) return null;
        var ordinal = 0;
        for (var index = 0; index < textureId; index++)
            if (PresenceFlags[index] == 1) ordinal++;
        return Materials[ordinal];
    }

    /// <summary>The material of <paramref name="textureId"/> for rendering; textures without one, or with an alpha mode above 2, are not supported.</summary>
    public static MaterialEntry Require(IMaterialTableType table, int textureId)
    {
        var material = table.Find(textureId) ?? throw new NotSupportedException("The texture has no material definition.");
        return material.AlphaMode <= 2 ? material : throw new NotSupportedException($"Unsupported material alpha mode {material.AlphaMode}.");
    }
}
