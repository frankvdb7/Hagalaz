using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IVoiceId
    {
        IVoiceOptional WithId(int id);
    }
}