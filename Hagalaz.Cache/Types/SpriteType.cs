using System;
using Hagalaz.Cache.Abstractions.Types;
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
        private bool _disposed;

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
        public Image<Rgba32> Image { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteType"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public SpriteType(int id)
            : this(id, 1, 1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteType"/> class with an image of the specified size.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        public SpriteType(int id, int width, int height)
        {
            Id = id;
            Image = new Image<Rgba32>(width, height);
        }

        /// <summary>
        /// Replaces the owned image with a copy of the supplied image, including every animation frame.
        /// The caller retains ownership of <paramref name="sourceImage"/>.
        /// </summary>
        /// <param name="sourceImage">The image to copy.</param>
        public void ReplaceImage(Image<Rgba32> sourceImage)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(sourceImage);

            var replacement = sourceImage.Clone();
            var previous = Image;
            Image = replacement;
            previous.Dispose();
        }

        /// <summary>
        /// Sets the frame.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="frame">The frame.</param>
        public void InsertFrame(int index, ImageFrame<Rgba32> frame)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Image.Frames.InsertFrame(index, frame);
            if (index == 0)
            {
                Image.Mutate(x => x.Resize(frame.Width, frame.Height));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Image.Dispose();
            _disposed = true;
        }

    }
}
