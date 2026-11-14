using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>
/// The revision-742 texture material table, index 26 file 0 member 0: a texture count, one presence flag per texture, then
/// eighteen byte columns, an int column and the alpha-mode column, each holding one value per present texture.
/// </summary>
public sealed class MaterialTableTypeCodec : ITypeCodec<IMaterialTableType>
{
    public IMaterialTableType Decode(int id, MemoryStream stream)
    {
        var reader = new DefinitionReader(stream, "material table");
        var table = new MaterialTableType(id) { PresenceFlags = reader.Bytes(reader.UShort()) };
        var present = table.PresenceFlags.Count(flag => flag == 1);
        if (stream.Length - stream.Position != present * (MaterialEntry.ByteColumnCount + 5))
            throw new InvalidDataException(stream.Length - stream.Position < present * (MaterialEntry.ByteColumnCount + 5)
                ? "Truncated material table." : "Material table has data after its last column.");

        for (var row = 0; row < present; row++) table.Materials.Add(new MaterialEntry());
        for (var column = 0; column < MaterialEntry.ByteColumnCount; column++)
            foreach (var material in table.Materials) material.ByteColumns[column] = (byte)reader.Byte();
        foreach (var material in table.Materials) material.EffectParameter2 = reader.Int();
        foreach (var material in table.Materials) material.AlphaMode = reader.Byte();
        return table;
    }

    public MemoryStream Encode(IMaterialTableType table)
    {
        var present = table.PresenceFlags.Count(flag => flag == 1);
        if (present != table.Materials.Count)
            throw new InvalidDataException($"The material table has {present} present textures but {table.Materials.Count} materials.");

        var stream = new MemoryStream();
        stream.UShort(table.PresenceFlags.Length);
        stream.Write(table.PresenceFlags);
        for (var column = 0; column < MaterialEntry.ByteColumnCount; column++)
            foreach (var material in table.Materials) stream.Byte(material.ByteColumns[column]);
        foreach (var material in table.Materials) stream.Int(material.EffectParameter2);
        foreach (var material in table.Materials) stream.Byte(material.AlphaMode);
        return stream;
    }
}
