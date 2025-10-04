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
        public Image<Rgba32> Image { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteType"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public SpriteType(int id)
        {
            Id = id;
            Image = new Image<Rgba32>(1, 1);
        }

        /// <summary>
        /// Sets the frame.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="frame">The frame.</param>
        public void InsertFrame(int index, ImageFrame<Rgba32> frame)
        {
            Image.Frames.InsertFrame(index, frame);
            if (index == 0)
            {
                Image.Mutate(x => x.Resize(frame.Width, frame.Height));
            }
        }

    }
}