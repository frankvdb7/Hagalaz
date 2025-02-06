using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.HintIcon
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHintIconEntityOptional : IHintIconOptional
    {
        IHintIconEntityOptional WithFlashSpeed(int flashSpeed);
    }
}