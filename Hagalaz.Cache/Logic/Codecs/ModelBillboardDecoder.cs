using System.IO;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Revision-742 Class207 definitions, index 29 archive 0.</summary>
public static class ModelBillboardDecoder
{
    public sealed record Definition(int TextureId, int HalfWidth, int HalfHeight, int BlendMode, bool HideFace, bool HideInSpecialPass);

    public static Definition Decode(Stream data)
    {
        using var reader = new BinaryReader(data, System.Text.Encoding.UTF8, leaveOpen: true);
        var textureId = -1;
        var halfWidth = 64;
        var halfHeight = 64;
        var blendMode = 2;
        var hideFace = false;
        var hideInSpecialPass = false;
        while (true)
        {
            var opcode = reader.ReadByte();
            switch (opcode)
            {
                case 0: return new(textureId, halfWidth, halfHeight, blendMode, hideFace, hideInSpecialPass);
                case 1:
                    textureId = Short();
                    if (textureId == 65535) textureId = -1;
                    break;
                case 2:
                    halfWidth = Short() + 1;
                    halfHeight = Short() + 1;
                    break;
                case 3: reader.ReadByte(); break;
                case 4: blendMode = reader.ReadByte(); break;
                case 5: reader.ReadByte(); break; // Class52's OpenGL path does not retain this field.
                case 6: hideInSpecialPass = true; break;
                case 7: hideFace = true; break;
                default: throw new InvalidDataException($"Unknown model billboard opcode {opcode}.");
            }
        }

        int Short() => (reader.ReadByte() << 8) | reader.ReadByte();
    }
}
