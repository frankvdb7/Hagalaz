using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Cache.Logic.Codecs
{
    public class SpriteTypeCodec : ISpriteTypeCodec
    {
        public ISpriteType Decode(int id, MemoryStream stream)
        {
            /* find the size of this sprite set */
            stream.Position = stream.Length - 2;

            int size = stream.ReadUnsignedShort() & 0xFFFF;

            /* allocate arrays to store info */
            int[] offsetsX = new int[size];
            int[] offsetsY = new int[size];
            int[] subWidths = new int[size];
            int[] subHeights = new int[size];

            /* read the width, height and palette size */
            stream.Position = stream.Length - size * 8 - 7;
            var Width = stream.ReadUnsignedShort() & 0xFFFF;
            var Height = stream.ReadUnsignedShort() & 0xFFFF;
            int[] palette = new int[stream.ReadUnsignedByte() + 1];

            /* read the offsets and dimensions of the individual sprites */
            for (var i = 0; i < size; i++)
            {
                offsetsX[i] = stream.ReadUnsignedShort() & 0xFFFF;
            }
            for (var i = 0; i < size; i++)
            {
                offsetsY[i] = stream.ReadUnsignedShort() & 0xFFFF;
            }
            for (var i = 0; i < size; i++)
            {
                subWidths[i] = stream.ReadUnsignedShort() & 0xFFFF;
            }
            for (var i = 0; i < size; i++)
            {
                subHeights[i] = stream.ReadUnsignedShort() & 0xFFFF;
            }

            /* read the palette */
            stream.Position = stream.Length - size * 8 - 7 - (palette.Length - 1) * 3;
            palette[0] = 0; /* transparent colour (black) */
            for (var index = 1; index < palette.Length; index++)
            {
                palette[index] = stream.ReadMedInt();
                if (palette[index] == 0)
                    palette[index] = 1;
            }

            var spriteType = new SpriteType(id)
            {
                Image = new Image<Rgba32>(Width, Height)
            };

            /* read the pixels themselves */
            stream.Position = 0;
            for (var i = 0; i < size; i++)
            {
                /* grab some frequently used values */
                int subWidth = subWidths[i], subHeight = subHeights[i];
                int offsetX = offsetsX[i], offsetY = offsetsY[i];

                var frameImage = new Image<Rgba32>(Width, Height);

                /* allocate an array for the palette indices */
                int[,] indices = new int[subWidth, subHeight];

                /* read the flags so we know whether to read horizontally or vertically */
                SpriteType.Flags flags = (SpriteType.Flags)stream.ReadUnsignedByte();

                /* now read the image */
                /* read the palette indices */
                if (flags.HasFlag(SpriteType.Flags.Vertical))
                {
                    for (var x = 0; x < subWidth; x++)
                    {
                        for (var y = 0; y < subHeight; y++)
                        {
                            indices[x, y] = stream.ReadUnsignedByte();
                        }
                    }
                }
                else
                {
                    for (var y = 0; y < subHeight; y++)
                    {
                        for (var x = 0; x < subWidth; x++)
                        {
                            indices[x, y] = stream.ReadUnsignedByte();
                        }
                    }
                }

                /* read the alpha (if there is alpha) and convert values to BGRA */
                if (flags.HasFlag(SpriteType.Flags.Alpha))
                {
                    if (flags.HasFlag(SpriteType.Flags.Vertical))
                    {
                        for (var x = 0; x < subWidth; x++)
                        {
                            for (var y = 0; y < subHeight; y++)
                            {
                                int alpha = stream.ReadUnsignedByte();
                                int c = palette[indices[x, y]];
                                var r = (byte)(c >> 16);
                                var g = (byte)(c >> 8);
                                var b = (byte)c;
                                frameImage[x + offsetX, y + offsetY] = new Rgba32(r, g, b, (byte)alpha);
                            }
                        }
                    }
                    else
                    {
                        for (var y = 0; y < subHeight; y++)
                        {
                            for (var x = 0; x < subWidth; x++)
                            {
                                int alpha = stream.ReadUnsignedByte();
                                int c = palette[indices[x, y]];
                                var r = (byte)(c >> 16);
                                var g = (byte)(c >> 8);
                                var b = (byte)c;
                                frameImage[x + offsetX, y + offsetY] = new Rgba32(r, g, b, (byte)alpha);
                            }
                        }
                    }
                }
                else
                {
                    for (var x = 0; x < subWidth; x++)
                    {
                        for (var y = 0; y < subHeight; y++)
                        {
                            int index = indices[x, y];
                            int c = palette[indices[x, y]];
                            var r = (byte)(c >> 16);
                            var g = (byte)(c >> 8);
                            var b = (byte)c;
                            frameImage[x + offsetX, y + offsetY] = new Rgba32(r, g, b, (byte)(index == 0 ? 0 : 255));
                        }
                    }
                }
                spriteType.Image.Frames.AddFrame(frameImage.Frames.RootFrame);
            }

            if (size > 0)
            {
                spriteType.Image.Frames.RemoveFrame(0);
            }

            return spriteType;
        }

        public MemoryStream Encode(ISpriteType instance)
        {
            using var pixelDataStream = new MemoryStream();
            using var paletteDataStream = new MemoryStream();
            using var metaDataStream = new MemoryStream();

            var paletteSet = new HashSet<Rgba32> { new Rgba32(0, 0, 0, 0) };
            bool hasAlpha = false;

            foreach (var frame in instance.Image.Frames)
            {
                for (int y = 0; y < frame.Height; y++)
                {
                    for (int x = 0; x < frame.Width; x++)
                    {
                        var rgba = frame[x, y];
                        if (rgba.A != 0 && rgba.A != 255)
                        {
                            hasAlpha = true;
                        }
                        if (paletteSet.Count < 256)
                        {
                            paletteSet.Add(rgba);
                        }
                    }
                }
            }

            var palette = paletteSet.ToList();
            var paletteMap = new Dictionary<Rgba32, byte>();
            for (int i = 0; i < palette.Count; i++)
            {
                paletteMap[palette[i]] = (byte)i;
            }

            var offsetsX = new int[instance.Image.Frames.Count];
            var offsetsY = new int[instance.Image.Frames.Count];
            var subWidths = new int[instance.Image.Frames.Count];
            var subHeights = new int[instance.Image.Frames.Count];

            for (int i = 0; i < instance.Image.Frames.Count; i++)
            {
                var frame = instance.Image.Frames[i];
                if (frame.Width != instance.Image.Width || frame.Height != instance.Image.Height)
                    throw new IOException("All frames must have the same dimensions!");

                var (minX, minY, width, height) = GetBoundingBox(frame);
                offsetsX[i] = minX;
                offsetsY[i] = minY;
                subWidths[i] = width;
                subHeights[i] = height;

                SpriteType.Flags flags = SpriteType.Flags.Vertical;
                if (hasAlpha)
                    flags |= SpriteType.Flags.Alpha;

                pixelDataStream.WriteByte((byte)flags);

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        pixelDataStream.WriteByte(paletteMap[frame[x + minX, y + minY]]);
                    }
                }

                if (hasAlpha)
                {
                    for (var x = 0; x < width; x++)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            pixelDataStream.WriteByte(frame[x + minX, y + minY].A);
                        }
                    }
                }
            }

            for (var i = 1; i < palette.Count; i++)
            {
                var color = palette[i];
                paletteDataStream.WriteMedInt(color.R << 16 | color.G << 8 | color.B);
            }

            metaDataStream.WriteShort(instance.Image.Width);
            metaDataStream.WriteShort(instance.Image.Height);
            metaDataStream.WriteByte(palette.Count - 1);

            foreach(var val in offsetsX) metaDataStream.WriteShort(val);
            foreach(var val in offsetsY) metaDataStream.WriteShort(val);
            foreach(var val in subWidths) metaDataStream.WriteShort(val);
            foreach(var val in subHeights) metaDataStream.WriteShort(val);

            var finalStream = new MemoryStream();

            pixelDataStream.Position = 0;
            paletteDataStream.Position = 0;
            metaDataStream.Position = 0;

            pixelDataStream.CopyTo(finalStream);
            paletteDataStream.CopyTo(finalStream);
            metaDataStream.CopyTo(finalStream);

            finalStream.WriteShort(instance.Image.Frames.Count);

            finalStream.Position = 0;
            return finalStream;
        }

        private (int, int, int, int) GetBoundingBox(ImageFrame<Rgba32> frame)
        {
            int minX = frame.Width;
            int minY = frame.Height;
            int maxX = 0;
            int maxY = 0;

            for (int y = 0; y < frame.Height; y++)
            {
                for (int x = 0; x < frame.Width; x++)
                {
                    if (frame[x, y].A != 0)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (minX > maxX || minY > maxY)
            {
                return (0, 0, 0, 0);
            }

            return (minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}