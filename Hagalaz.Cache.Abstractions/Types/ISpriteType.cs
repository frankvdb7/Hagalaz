using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Cache.Abstractions.Types
{
    public interface ISpriteType : IType
    {
        Image<Rgba32> Image { get; }

        void InsertFrame(int index, ImageFrame<Rgba32> frame);
    }
}