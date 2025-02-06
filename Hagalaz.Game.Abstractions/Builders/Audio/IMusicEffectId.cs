using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMusicEffectId
    {
        IMusicEffectOptional WithId(int id);
    }
}