using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IVoiceOptional : ISoundBuild
    {
        IVoiceOptional WithVolume(int volume);
        IVoiceOptional WithRepeatCount(int repeat);
        IVoiceOptional WithDelay(int delay);
    }
}