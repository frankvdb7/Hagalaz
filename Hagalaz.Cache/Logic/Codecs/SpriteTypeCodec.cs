using System;
using System.Collections.Generic;
using System.IO;
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
        public ISpriteType Decode(MemoryStream stream)
        {
            var spriteType = new SpriteType(0);

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

            spriteType.Image = new Image<Bgra5551>(Width, Height);

            /* read the pixels themselves */
            stream.Position = 0;
            for (var id = 0; id < size; id++)
            {
                /* grab some frequently used values */
                int subWidth = subWidths[id], subHeight = subHeights[id];
                int offsetX = offsetsX[id], offsetY = offsetsY[id];

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
                                spriteType.Image[x + offsetX, y + offsetY] = new Color(new Rgba32(r, g, b, (byte)alpha)).ToPixel<Bgra5551>();
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
                                spriteType.Image[x + offsetX, y + offsetY] = new Color(new Rgba32(r, g, b, (byte)alpha)).ToPixel<Bgra5551>();
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
                            spriteType.Image[x + offsetX, y + offsetY] = new Color(new Rgba32(r, g, b, (byte)(index == 0 ? 0 : 255))).ToPixel<Bgra5551>();
                        }
                    }
                }
            }
            return spriteType;
        }

        public MemoryStream Encode(ISpriteType instance)
        {
            if (instance is not SpriteType sprite)
            {
                throw new ArgumentException("The provided instance is not a valid SpriteType.", nameof(instance));
            }

            var pixelDataStream = new MemoryStream();
            var paletteDataStream = new MemoryStream();
            var metaDataStream = new MemoryStream();
            var finalStream = new MemoryStream();

            /* set up some variables */
            var palette = new List<Color>
            {
                Color.Transparent
            };
            var transparentPixel = Color.Transparent.ToPixel<Rgba32>();

            // Step 1: Build a global palette for the entire image
            bool hasAlpha = false;
            foreach (var frame in sprite.Image.Frames)
            {
                for (int y = 0; y < frame.Height; y++)
                {
                    for (int x = 0; x < frame.Width; x++)
                    {
                        var rgba = transparentPixel;
                        sprite.Image[x,y].ToRgba32(ref rgba);

                        if (rgba.A != byte.MinValue && rgba.A != byte.MaxValue)
                            hasAlpha = true;

                        if (!palette.Contains(rgba) && palette.Count <= byte.MaxValue)
                        {
                            palette.Add(rgba);
                        }
                    }
                }
            }

            // Step 2: Write pixel data to a temporary stream
            foreach (var frame in sprite.Image.Frames)
            {
                /* check if we can encode this */
                if (frame.Width != sprite.Image.Width || frame.Height != sprite.Image.Height)
                    throw new IOException("All frames must have the same dimensions!");

                SpriteType.Flags flags = SpriteType.Flags.Vertical;
                if (hasAlpha)
                    flags |= SpriteType.Flags.Alpha;

                pixelDataStream.WriteByte((byte)flags);

                // Write pixel indices
                for (var x = 0; x < frame.Width; x++)
                {
                    for (var y = 0; y < frame.Height; y++)
                    {
                        var rgba = transparentPixel;
                        sprite.Image[x,y].ToRgba32(ref rgba);
                        pixelDataStream.WriteByte((byte)palette.IndexOf(rgba));
                    }
                }

                // Write alpha channel if this sprite has one
                if (hasAlpha)
                {
                    for (var x = 0; x < frame.Width; x++)
                    {
                        for (var y = 0; y < frame.Height; y++)
                        {
                            var rgba = transparentPixel;
                            sprite.Image[x,y].ToRgba32(ref rgba);
                            pixelDataStream.WriteByte(rgba.A);
                        }
                    }
                }
            }

            // Step 3: Write palette data to a temporary stream
            for (var i = 1; i < palette.Count; i++)
            {
                var color = palette[i].ToPixel<Rgb24>();
                paletteDataStream.WriteMedInt(color.R << 16 | color.G << 8 | color.B);
            }

            // Step 4: Write metadata to a temporary stream
            metaDataStream.WriteShort(sprite.Image.Width);
            metaDataStream.WriteShort(sprite.Image.Height);
            metaDataStream.WriteByte(palette.Count - 1);

            for (var i = 0; i < sprite.Image.Frames.Count; i++)
            {
                metaDataStream.WriteShort(0); // offset x
            }
            for (var i = 0; i < sprite.Image.Frames.Count; i++)
            {
                metaDataStream.WriteShort(0); // offset y
            }
            for (var i = 0; i < sprite.Image.Frames.Count; i++)
            {
                metaDataStream.WriteShort(sprite.Image.Width);
            }
            for (var i = 0; i < sprite.Image.Frames.Count; i++)
            {
                metaDataStream.WriteShort(sprite.Image.Height);
            }

            // Step 5: Assemble the final stream
            finalStream.Write(pixelDataStream.ToArray());
            finalStream.Write(paletteDataStream.ToArray());
            finalStream.Write(metaDataStream.ToArray());
            finalStream.WriteShort(sprite.Image.Frames.Count);

            return finalStream;
        }
    }
}