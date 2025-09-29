using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="IType" />
    public class SpriteType : IType, ISpriteType
    {
        /// <summary>
        /// 
        /// </summary>
        [Flags]
        public enum Flags : byte
        {
            /// <summary>
            /// The vertical
            /// </summary>
            Vertical = 0x01,
            /// <summary>
            /// The alpha
            /// </summary>
            Alpha = 0x02
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; }
        /// <summary>
        /// Gets the actual image.
        /// </summary>
        public Image<Bgra5551> Image { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteType"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public SpriteType(int id)
        {
            Id = id;
            Image = new Image<Bgra5551>(1, 1);
        }

        /// <summary>
        /// Decodes the specified buffer.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public void Decode(MemoryStream stream)
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

            Image = new Image<Bgra5551>(Width, Height);

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
                Flags flags = (Flags)stream.ReadUnsignedByte();

                /* now read the image */
                /* read the palette indices */
                if (flags.HasFlag(Flags.Vertical))
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
                if (flags.HasFlag(Flags.Alpha))
                {
                    if (flags.HasFlag(Flags.Vertical))
                    {
                        for (var x = 0; x < subWidth; x++)
                        {
                            for (var y = 0; y < subHeight; y++)
                            {
                                int alpha = stream.ReadUnsignedByte();
                                Image[x + offsetX, y + offsetY] = new Color(new Argb32((uint)(alpha << 24 | palette[indices[x, y]]))).ToPixel<Bgra5551>();
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
                                Image[x + offsetX, y + offsetY] = new Color(new Argb32((uint)(alpha << 24 | palette[indices[x, y]]))).ToPixel<Bgra5551>();
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
                            if (index == 0)
                            {
                                Image[x + offsetX, y + offsetY] = new Color().ToPixel<Bgra5551>();
                            }
                            else
                            {
                                Image[x + offsetX, y + offsetY] = new Color(new Argb32((uint)(0xFF000000 << 24 | palette[indices[x, y]]))).ToPixel<Bgra5551>();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the frame.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="frame">The frame.</param>
        public void InsertFrame(int index, ImageFrame frame)
        {
            Image.Frames.InsertFrame(index, frame);
            if (index == 0)
            {
                Image.Mutate(x => x.Resize(frame.Width, frame.Height));
            }
        }

        /// <summary>
        /// Encodes this instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.IO.IOException">All frames must have the same dimensions!</exception>
        public MemoryStream Encode()
        {
            var buffer = new MemoryStream();
            /* set up some variables */
            var palette = new List<Color>
            {
                Color.Transparent
            };
            /* transparent pixel */
            var transparentPixel = Color.Transparent.ToPixel<Rgba32>();


            /* write the sprites */
            foreach (var frame in Image.Frames)
            {
                /* check if we can encode this */
                if (frame.Width != Image.Width || frame.Height != Image.Height)
                    throw new IOException("All frames must have the same dimensions!");

                /* loop through all the pixels constructing a palette */
                Flags flags = Flags.Vertical; // TODO - Support horizontal encoding
                for (int x = 0; x < Image.Width; x++)
                {
                    for (int y = 0; y < Image.Height; y++)
                    {
                        /* grab the colour of this pixel */
                        var rgba = transparentPixel;
                        Image[x, y].ToRgba32(ref rgba);


                        /* we need an alpha channel to encode this image */
                        if (rgba.A != byte.MinValue && rgba.A != byte.MaxValue)
                            flags |= Flags.Alpha;

                        /* add the colour to the palette if it isn't already in the palette */
                        if (!palette.Contains(rgba) && palette.Count <= byte.MaxValue)
                        {
                            palette.Add(rgba);
                        }
                    }
                }

                /* write this sprite */
                buffer.WriteByte((byte)flags);

                for (var x = 0; x < Image.Width; x++)
                {
                    for (var y = 0; y < Image.Height; y++)
                    {
                        var rgba = transparentPixel;
                        Image[x, y].ToRgba32(ref rgba);

                        if (!flags.HasFlag(Flags.Alpha) && rgba.A == 0)
                            buffer.WriteByte(0);
                        else
                            buffer.WriteByte(palette.IndexOf(rgba));
                    }
                }

                /* write the alpha channel if this sprite has one */
                if (flags.HasFlag(Flags.Alpha))
                {
                    for (var x = 0; x < Image.Width; x++)
                    {
                        for (var y = 0; y < Image.Height; y++)
                        {
                            var rgba = transparentPixel;
                            Image[x, y].ToRgba32(ref rgba);

                            buffer.WriteByte(rgba.A);
                        }
                    }
                }
            }

            /* write the palette */
            for (var i = 1; i < palette.Count; i++)
            {
                var color = palette[i].ToPixel<Rgba32>();
                buffer.WriteByte(color.R);
                buffer.WriteByte(color.G);
                buffer.WriteByte(color.B);
            }

            /* write the max width, height and palette size */
            buffer.WriteShort(Image.Width);
            buffer.WriteShort(Image.Height);
            buffer.WriteByte(palette.Count - 1);

            /* write the individual offsets and dimensions */
            for (var i = 0; i < Image.Frames.Count; i++)
            {
                buffer.WriteShort(0); // offset x
                buffer.WriteShort(0); // offset y
                buffer.WriteShort(Image.Width);
                buffer.WriteShort(Image.Height);
            }

            /* write the number of frames */
            buffer.WriteShort(Image.Frames.Count);

            return buffer;
        }
    }
}
