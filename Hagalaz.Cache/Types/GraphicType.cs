using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// </summary>
    public class GraphicType
    {
        /// <summary>
        ///     Gets the identifier.
        /// </summary>
        public int ID { get; }

        /// <summary>
        ///     Gets an int255.
        /// </summary>
        public int Contrast { get; private set; }

        /// <summary>
        ///     Gets an int256.
        /// </summary>
        public int Ambient { get; private set; }

        /// <summary>
        ///     Gets an int257.
        /// </summary>
        public int ResizeX { get; private set; }

        /// <summary>
        ///     Gets an int258.
        /// </summary>
        public int Rotation { get; private set; }

        /// <summary>
        ///     Gets a short array259.
        /// </summary>
        public short[] RecolorToFind { get; private set; }

        /// <summary>
        ///     Gets a byte260.
        /// </summary>
        public byte AByte260 { get; private set; }

        /// <summary>
        ///     Gets an int262.
        /// </summary>
        public int AnInt262 { get; }

        /// <summary>
        ///     Gets an int264.
        /// </summary>
        public int AnimationID { get; private set; }

        /// <summary>
        ///     Gets an int265.
        /// </summary>
        public int AnInt265 { get; private set; }

        /// <summary>
        ///     Gets a short array266.
        /// </summary>
        public short[] RetextureToReplace { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [a boolean267].
        /// </summary>
        public bool ABoolean267 { get; private set; }

        /// <summary>
        ///     Gets a short array268.
        /// </summary>
        public short[] RetextureToFind { get; private set; }

        /// <summary>
        ///     Gets an int269.
        /// </summary>
        public int DefaultModelID { get; private set; }

        /// <summary>
        ///     Gets a short array271.
        /// </summary>
        public short[] RecolorToReplace { get; private set; }

        /// <summary>
        ///     Gets an int272.
        /// </summary>
        public int ResizeY { get; private set; }

        /// <summary>
        ///     Gets a byte array4428.
        /// </summary>
        public byte[] AByteArray4428 { get; private set; }

        /// <summary>
        ///     Gets a byte array4433.
        /// </summary>
        public byte[] AByteArray4433 { get; private set; }

        /// <summary>
        ///     Construct's new graphic definition.
        /// </summary>
        /// <param name="id">ID of the graphic.</param>
        public GraphicType(int id)
        {
            ID = id;
            AnimationID = -1;
            AnInt265 = -1;
            ResizeX = 128;
            ResizeY = 128;
        }

        /// <summary>
        ///     Parse's definition from given buffer.
        /// </summary>
        /// <param name="buffer">Buffer from which definition should be parsed.</param>
        private void ParseDefinition(MemoryStream buffer)
        {
            for (var opcode = (byte)buffer.ReadUnsignedByte(); opcode != 0; opcode = (byte)buffer.ReadUnsignedByte())
            {
                ParseOpcode(opcode, buffer);
            }
        }

        /// <summary>
        ///     Parse's animationdefinition opcode.
        /// </summary>
        /// <param name="opcode">Opcode which should be parsed.</param>
        /// <param name="buffer">Buffer from which additional data will be readed.</param>
        private void ParseOpcode(byte opcode, MemoryStream buffer)
        {
            if (opcode == 1)
            {
                DefaultModelID = buffer.ReadBigSmart();
            }
            else if (opcode == 2)
            {
                AnimationID = buffer.ReadBigSmart();
            }
            else if (opcode == 4)
            {
                ResizeX = buffer.ReadUnsignedShort();
            }
            else if (opcode == 5)
            {
                ResizeY = buffer.ReadUnsignedShort();
            }
            else if (opcode != 6)
            {
                if (opcode == 7)
                {
                    Ambient = buffer.ReadUnsignedByte();
                }
                else
                {
                    if (opcode == 8)
                    {
                        Contrast = buffer.ReadUnsignedByte();
                    }
                    else if (opcode == 9)
                    {
                        AByte260 = 3;
                        AnInt265 = 8224;
                    }
                    else if (opcode != 10)
                    {
                        if (opcode != 11)
                        {
                            if (opcode == 12)
                            {
                                AByte260 = 4;
                            }
                            else
                            {
                                if (opcode == 13)
                                {
                                    AByte260 = 5;
                                }
                                else if (opcode == 14)
                                {
                                    AByte260 = 2;
                                    AnInt265 = buffer.ReadUnsignedByte() * 256;
                                }
                                else if (opcode != 15)
                                {
                                    if (opcode == 16)
                                    {
                                        AByte260 = 3;
                                        AnInt265 = buffer.ReadInt();
                                    }
                                    else
                                    {
                                        if (opcode == 40)
                                        {
                                            var length = buffer.ReadUnsignedByte();
                                            RecolorToFind = new short[length];
                                            RecolorToReplace = new short[length];
                                            for (var i10 = 0; i10 < length; i10++)
                                            {
                                                RecolorToReplace[i10] = (short)buffer.ReadUnsignedShort();
                                                RecolorToFind[i10] = (short)buffer.ReadUnsignedShort();
                                            }
                                        }
                                        else
                                        {
                                            if (opcode == 41)
                                            {
                                                var length = buffer.ReadUnsignedByte();
                                                RetextureToFind = new short[length];
                                                RetextureToReplace = new short[length];
                                                for (var i8 = 0; i8 < length; i8++)
                                                {
                                                    RetextureToReplace[i8] = (short)buffer.ReadUnsignedShort();
                                                    RetextureToFind[i8] = (short)buffer.ReadUnsignedShort();
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

                                                AByteArray4428 = new byte[i18];
                                                byte i20 = 0;
                                                for (var i21 = 0; i21 < i18; i21++)
                                                {
                                                    if ((i17 & (1 << i21)) > 0)
                                                    {
                                                        AByteArray4428[i21] = i20;
                                                        i20++;
                                                    }
                                                    else
                                                    {
                                                        AByteArray4428[i21] = unchecked((byte)-1);
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

                                                AByteArray4433 = new byte[i23];
                                                byte i25 = 0;
                                                for (var i26 = 0; i26 < i23; i26++)
                                                {
                                                    if ((i22 & (1 << i26)) > 0)
                                                    {
                                                        AByteArray4433[i26] = i25;
                                                        i25++;
                                                    }
                                                    else
                                                    {
                                                        AByteArray4433[i26] = unchecked((byte)-1);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    AByte260 = 3;
                                    AnInt265 = buffer.ReadUnsignedShort();
                                }
                            }
                        }
                        else
                        {
                            AByte260 = 1;
                        }
                    }
                    else
                    {
                        ABoolean267 = true;
                    }
                }
            }
            else
            {
                Rotation = buffer.ReadUnsignedShort();
            }
        }


        /// <summary>
        ///     Get's animation's count in given owner.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <returns>System.Int32.</returns>
        public static int GetGraphicsCount(CacheApi cache)
        {
            var lastID = cache.GetFileCount(21) - 1;
            return (lastID * 127) + cache.GetFileCount(21, lastID);
        }

        /// <summary>
        ///     Get's animation definition from given owner.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="graphicId">ID of the animation.</param>
        /// <returns>AnimationDefinition.</returns>
        public static GraphicType GetGraphicDefinition(CacheApi cache, int graphicId)
        {
            var definition = new GraphicType(graphicId);
            using (var data = cache.Read(21, (int)((uint)graphicId >> 7), graphicId & 0xFF))
            {
                definition.ParseDefinition(data);
            }

            return definition;
        }
    }
}