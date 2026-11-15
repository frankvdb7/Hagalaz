using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Revision-742 Class207 model billboards, index 29 archive 0. Encodes opcodes in ascending order.</summary>
public sealed class BillboardTypeCodec : ITypeCodec<IBillboardType>
{
    public IBillboardType Decode(int id, MemoryStream stream)
    {
        var type = new BillboardType(id);
        var reader = new DefinitionReader(stream, "model billboard");
        for (var opcode = reader.Byte(); opcode != 0; opcode = reader.Byte())
        {
            switch (opcode)
            {
                case 1: type.TextureId = reader.UShort(); break;
                case 2: type.Size = new(reader.UShort(), reader.UShort()); break;
                case 3: type.Opcode3 = reader.Byte(); break;
                case 4: type.BlendMode = reader.Byte(); break;
                case 5: type.Opcode5 = reader.Byte(); break;
                case 6: type.HideInSpecialPass = true; break;
                case 7: type.HideFace = true; break;
                default: throw reader.UnknownOpcode(opcode);
            }
        }
        return type;
    }

    public MemoryStream Encode(IBillboardType type)
    {
        var stream = new MemoryStream();
        if (type.TextureId is { } texture) { stream.Byte(1); stream.UShort(texture); }
        if (type.Size is { } size) { stream.Byte(2); stream.UShort(size.Width); stream.UShort(size.Height); }
        if (type.Opcode3 is { } opcode3) { stream.Byte(3); stream.Byte(opcode3); }
        if (type.BlendMode is { } blendMode) { stream.Byte(4); stream.Byte(blendMode); }
        if (type.Opcode5 is { } opcode5) { stream.Byte(5); stream.Byte(opcode5); }
        if (type.HideInSpecialPass) stream.Byte(6);
        if (type.HideFace) stream.Byte(7);
        stream.Byte(0);
        return stream;
    }
}
