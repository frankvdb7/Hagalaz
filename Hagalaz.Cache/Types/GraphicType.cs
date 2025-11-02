using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    public class GraphicType : IGraphicType
    {
        public int Id { get; }
        public int Contrast { get; internal set; }
        public int Ambient { get; internal set; }
        public int ResizeX { get; internal set; }
        public int Rotation { get; internal set; }
        public short[] RecolorToFind { get; internal set; }
        public byte AByte260 { get; internal set; }
        public int AnInt262 { get; internal set; }
        public int AnimationID { get; internal set; }
        public int AnInt265 { get; internal set; }
        public short[] RetextureToReplace { get; internal set; }
        public bool ABoolean267 { get; internal set; }
        public short[] RetextureToFind { get; internal set; }
        public int DefaultModelID { get; internal set; }
        public short[] RecolorToReplace { get; internal set; }
        public int ResizeY { get; internal set; }
        public byte[] AByteArray4428 { get; internal set; }
        public byte[] AByteArray4433 { get; internal set; }

        public GraphicType(int id)
        {
            Id = id;
            AnimationID = -1;
            AnInt265 = -1;
            ResizeX = 128;
            ResizeY = 128;
            RecolorToFind = [];
            RetextureToReplace = [];
            RetextureToFind = [];
            RecolorToReplace = [];
            AByteArray4428 = [];
            AByteArray4433 = [];
        }
    }
}