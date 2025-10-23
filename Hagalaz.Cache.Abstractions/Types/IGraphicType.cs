using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Abstractions.Types
{
    public interface IGraphicType : IType
    {
        int Contrast { get; }
        int Ambient { get; }
        int ResizeX { get; }
        int Rotation { get; }
        short[] RecolorToFind { get; }
        byte AByte260 { get; }
        int AnInt262 { get; }
        int AnimationID { get; }
        int AnInt265 { get; }
        short[] RetextureToReplace { get; }
        bool ABoolean267 { get; }
        short[] RetextureToFind { get; }
        int DefaultModelID { get; }
        short[] RecolorToReplace { get; }
        int ResizeY { get; }
        byte[] AByteArray4428 { get; }
        byte[] AByteArray4433 { get; }
    }
}