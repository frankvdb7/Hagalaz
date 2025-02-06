using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Animation
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAnimationOptional : IAnimationBuild
    {
        IAnimationOptional WithDelay(int delay);
        IAnimationOptional WithPriority(int priority);
    }
}