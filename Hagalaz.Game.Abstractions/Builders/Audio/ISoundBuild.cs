using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Sound;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISoundBuild
    {
        ISound Build();
    }
}