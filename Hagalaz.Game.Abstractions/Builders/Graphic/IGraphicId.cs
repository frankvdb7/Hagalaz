using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Graphic
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGraphicId
    {
        IGraphicOptional WithId(int id);
    }
}