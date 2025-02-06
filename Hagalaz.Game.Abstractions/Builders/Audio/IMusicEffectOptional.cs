using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMusicEffectOptional : ISoundBuild
    {
        IMusicEffectOptional WithVolume(int volume);
    }
}