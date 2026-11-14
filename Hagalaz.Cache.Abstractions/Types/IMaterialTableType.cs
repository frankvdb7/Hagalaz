using System.Collections.Generic;

namespace Hagalaz.Cache.Abstractions.Types;

/// <summary>
/// The texture material table (index 26, file 0, member 0): one presence flag per texture, then one row per present texture,
/// stored column by column.
/// </summary>
public interface IMaterialTableType : IType
{
    /// <summary>One flag per texture ID, as stored; a texture has a material when its flag is 1.</summary>
    byte[] PresenceFlags { get; set; }
    /// <summary>The materials of the present textures, in texture ID order.</summary>
    IList<MaterialEntry> Materials { get; }

    /// <summary>The material of <paramref name="textureId"/>, or <c>null</c> when it has none.</summary>
    MaterialEntry? Find(int textureId);
}

/// <summary>One texture's material: eighteen byte columns, an int column and the alpha mode, as stored.</summary>
public sealed class MaterialEntry
{
    public const int ByteColumnCount = 18;

    /// <summary>Byte columns 0-17. Known: 3 brightness, 4 colour blend, 5 effect, 6 effect parameter, 9/10 scroll, 14/15 repeat, 16 HDR.</summary>
    public byte[] ByteColumns { get; set; } = new byte[ByteColumnCount];
    /// <summary>Column 18, an int whose meaning is unknown.</summary>
    public int EffectParameter2 { get; set; }
    /// <summary>Column 19: alpha mode.</summary>
    public int AlphaMode { get; set; }

    public int Brightness => ByteColumns[3];
    public int ColorBlend => ByteColumns[4];
    public int EffectId => ByteColumns[5];
    public int EffectParameter => ByteColumns[6];
    public int ScrollU => (sbyte)ByteColumns[9];
    public int ScrollV => (sbyte)ByteColumns[10];
    public bool RepeatU => ByteColumns[14] == 1;
    public bool RepeatV => ByteColumns[15] == 1;
    public bool HasHdr => ByteColumns[16] == 1;
}
