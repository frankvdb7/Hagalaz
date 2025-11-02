using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs
{
    public class GraphicTypeCodec : IGraphicTypeCodec
    {
        public IGraphicType Decode(int id, MemoryStream stream)
        {
            var graphicType = new GraphicType(id);
            ParseDefinition(graphicType, stream);
            return graphicType;
        }

        public MemoryStream Encode(IGraphicType type)
        {
            var graphicType = (GraphicType)type;
            var writer = new MemoryStream();

            if (graphicType.DefaultModelID != 0)
            {
                writer.WriteByte(1);
                writer.WriteBigSmart(graphicType.DefaultModelID);
            }

            if (graphicType.AnimationID != -1)
            {
                writer.WriteByte(2);
                writer.WriteBigSmart(graphicType.AnimationID);
            }

            if (graphicType.ResizeX != 128)
            {
                writer.WriteByte(4);
                writer.WriteShort(graphicType.ResizeX);
            }

            if (graphicType.ResizeY != 128)
            {
                writer.WriteByte(5);
                writer.WriteShort(graphicType.ResizeY);
            }

            if (graphicType.Rotation != 0)
            {
                writer.WriteByte(6);
                writer.WriteShort(graphicType.Rotation);
            }

            if (graphicType.Ambient != 0)
            {
                writer.WriteByte(7);
                writer.WriteByte((byte)graphicType.Ambient);
            }

            if (graphicType.Contrast != 0)
            {
                writer.WriteByte(8);
                writer.WriteByte((byte)graphicType.Contrast);
            }

            if (graphicType.ABoolean267)
            {
                writer.WriteByte(10);
            }

            if (graphicType.AByte260 == 1)
            {
                writer.WriteByte(11);
            }
            else if (graphicType.AByte260 == 4)
            {
                writer.WriteByte(12);
            }
            else if (graphicType.AByte260 == 5)
            {
                writer.WriteByte(13);
            }
            else if (graphicType.AByte260 == 2)
            {
                writer.WriteByte(14);
                writer.WriteByte((byte)(graphicType.AnInt265 / 256));
            }
            else if (graphicType.AByte260 == 3)
            {
                if (graphicType.AnInt265 == 8224)
                {
                    writer.WriteByte(9);
                }
                else if (graphicType.AnInt265 >= 0 && graphicType.AnInt265 <= ushort.MaxValue)
                {
                    writer.WriteByte(15);
                    writer.WriteShort(graphicType.AnInt265);
                }
                else
                {
                    writer.WriteByte(16);
                    writer.WriteInt(graphicType.AnInt265);
                }
            }

            if (graphicType.RecolorToFind.Length > 0)
            {
                writer.WriteByte(40);
                writer.WriteByte((byte)graphicType.RecolorToFind.Length);
                for (var i = 0; i < graphicType.RecolorToFind.Length; i++)
                {
                    writer.WriteShort(graphicType.RecolorToReplace[i]);
                    writer.WriteShort(graphicType.RecolorToFind[i]);
                }
            }

            if (graphicType.RetextureToFind.Length > 0)
            {
                writer.WriteByte(41);
                writer.WriteByte((byte)graphicType.RetextureToFind.Length);
                for (var i = 0; i < graphicType.RetextureToFind.Length; i++)
                {
                    writer.WriteShort(graphicType.RetextureToReplace[i]);
                    writer.WriteShort(graphicType.RetextureToFind[i]);
                }
            }

            if (graphicType.AByteArray4428.Length > 0)
            {
                writer.WriteByte(44);
                ushort value = 0;
                for (int i = 0; i < graphicType.AByteArray4428.Length; i++)
                {
                    if (graphicType.AByteArray4428[i] != 255)
                    {
                        value |= (ushort)(1 << i);
                    }
                }
                writer.WriteShort(value);
            }

            if (graphicType.AByteArray4433.Length > 0)
            {
                writer.WriteByte(45);
                ushort value = 0;
                for (int i = 0; i < graphicType.AByteArray4433.Length; i++)
                {
                    if (graphicType.AByteArray4433[i] != 255)
                    {
                        value |= (ushort)(1 << i);
                    }
                }
                writer.WriteShort(value);
            }

            writer.WriteByte(0);
            return writer;
        }

        private void ParseDefinition(GraphicType graphicType, MemoryStream buffer)
        {
            for (var opcode = (byte)buffer.ReadUnsignedByte(); opcode != 0; opcode = (byte)buffer.ReadUnsignedByte())
            {
                ParseOpcode(graphicType, opcode, buffer);
            }
        }

        private void ParseOpcode(GraphicType graphicType, byte opcode, MemoryStream buffer)
        {
            if (opcode == 1)
            {
                graphicType.DefaultModelID = buffer.ReadBigSmart();
            }
            else if (opcode == 2)
            {
                graphicType.AnimationID = buffer.ReadBigSmart();
            }
            else if (opcode == 4)
            {
                graphicType.ResizeX = buffer.ReadUnsignedShort();
            }
            else if (opcode == 5)
            {
                graphicType.ResizeY = buffer.ReadUnsignedShort();
            }
            else if (opcode == 6)
            {
                graphicType.Rotation = buffer.ReadUnsignedShort();
            }
            else if (opcode == 7)
            {
                graphicType.Ambient = buffer.ReadUnsignedByte();
            }
            else if (opcode == 8)
            {
                graphicType.Contrast = buffer.ReadUnsignedByte();
            }
            else if (opcode == 9)
            {
                graphicType.AByte260 = 3;
                graphicType.AnInt265 = 8224;
            }
            else if (opcode == 10)
            {
                graphicType.ABoolean267 = true;
            }
            else if (opcode == 11)
            {
                graphicType.AByte260 = 1;
            }
            else if (opcode == 12)
            {
                graphicType.AByte260 = 4;
            }
            else if (opcode == 13)
            {
                graphicType.AByte260 = 5;
            }
            else if (opcode == 14)
            {
                graphicType.AByte260 = 2;
                graphicType.AnInt265 = buffer.ReadUnsignedByte() * 256;
            }
            else if (opcode == 15)
            {
                graphicType.AByte260 = 3;
                graphicType.AnInt265 = buffer.ReadUnsignedShort();
            }
            else if (opcode == 16)
            {
                graphicType.AByte260 = 3;
                graphicType.AnInt265 = buffer.ReadInt();
            }
            else if (opcode == 40)
            {
                var length = buffer.ReadUnsignedByte();
                graphicType.RecolorToFind = new short[length];
                graphicType.RecolorToReplace = new short[length];
                for (var i = 0; i < length; i++)
                {
                    graphicType.RecolorToReplace[i] = (short)buffer.ReadUnsignedShort();
                    graphicType.RecolorToFind[i] = (short)buffer.ReadUnsignedShort();
                }
            }
            else if (opcode == 41)
            {
                var length = buffer.ReadUnsignedByte();
                graphicType.RetextureToFind = new short[length];
                graphicType.RetextureToReplace = new short[length];
                for (var i = 0; i < length; i++)
                {
                    graphicType.RetextureToReplace[i] = (short)buffer.ReadUnsignedShort();
                    graphicType.RetextureToFind[i] = (short)buffer.ReadUnsignedShort();
                }
            }
            else if (opcode == 44)
            {
                var i17 = buffer.ReadUnsignedShort();
                var i18 = 0;
                for (var i19 = i17; i19 > 0; i19 >>= 1)
                {
                    i18++;
                }

                graphicType.AByteArray4428 = new byte[i18];
                byte i20 = 0;
                for (var i21 = 0; i21 < i18; i21++)
                {
                    if ((i17 & (1 << i21)) > 0)
                    {
                        graphicType.AByteArray4428[i21] = i20;
                        i20++;
                    }
                    else
                    {
                        graphicType.AByteArray4428[i21] = unchecked((byte)-1);
                    }
                }
            }
            else if (opcode == 45)
            {
                var i22 = buffer.ReadUnsignedShort();
                var i23 = 0;
                for (var i24 = i22; i24 > 0; i24 >>= 1)
                {
                    i23++;
                }

                graphicType.AByteArray4433 = new byte[i23];
                byte i25 = 0;
                for (var i26 = 0; i26 < i23; i26++)
                {
                    if ((i22 & (1 << i26)) > 0)
                    {
                        graphicType.AByteArray4433[i26] = i25;
                        i25++;
                    }
                    else
                    {
                        graphicType.AByteArray4433[i26] = unchecked((byte)-1);
                    }
                }
            }
        }
    }
}