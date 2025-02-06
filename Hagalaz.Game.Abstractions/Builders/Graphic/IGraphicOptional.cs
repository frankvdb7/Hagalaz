using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Graphic
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGraphicOptional : IGraphicBuild
    {
        IGraphicOptional WithDelay(int delay);
        IGraphicOptional WithHeight(int height);
        IGraphicOptional WithRotation(int rotation);
    }
}