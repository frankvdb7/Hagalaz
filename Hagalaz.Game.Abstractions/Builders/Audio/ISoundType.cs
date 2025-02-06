using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISoundType
    {
        ISoundId AsSound();
        IMusicEffectId AsMusicEffect();
        IVoiceId AsVoice();
    }
}