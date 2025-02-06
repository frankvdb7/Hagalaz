using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Animation
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAnimationId
    {
        IAnimationOptional WithId(int id);
    }
}