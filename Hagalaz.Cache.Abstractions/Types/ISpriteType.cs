using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines the contract for a sprite type, which represents a single sprite or a collection of frames
    /// for an animated sprite from the game cache.
    /// </summary>
    public interface ISpriteType : IType
    {
        /// <summary>
        /// Gets the underlying <see cref="Image{Rgba32}"/> that contains the sprite's pixel data.
        /// For animated sprites, this image will contain multiple frames.
        /// </summary>
        Image<Rgba32> Image { get; }

        /// <summary>
        /// Inserts a new frame into the sprite's image at the specified index.
        /// This is typically used during the decoding process to build an animated sprite frame by frame.
        /// </summary>
        /// <param name="index">The zero-based index at which the frame should be inserted.</param>
        /// <param name="frame">The <see cref="ImageFrame{Rgba32}"/> to insert.</param>
        void InsertFrame(int index, ImageFrame<Rgba32> frame);
    }
}