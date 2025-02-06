using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISoundOptional : ISoundBuild
    {
        ISoundOptional WithVolume(int volume);
        ISoundOptional WithRepeatCount(int count);
        ISoundOptional WithDelay(int delay);
        ISoundOptional WithPlaybackSpeed(int playbackSpeed);
    }
}