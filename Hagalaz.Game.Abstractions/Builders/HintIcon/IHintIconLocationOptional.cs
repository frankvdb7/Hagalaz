using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.HintIcon
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHintIconLocationOptional : IHintIconOptional
    {
        IHintIconLocationOptional WithHeight(int height);
        IHintIconLocationOptional WithViewDistance(int viewDistance);
        IHintIconLocationOptional WithDirection(HintIconDirection direction);
    }
}