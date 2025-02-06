using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Glow
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGlowOptional : IGlowBuild
    {
        IGlowOptional WithRed(byte red);
        IGlowOptional WithGreen(byte green);
        IGlowOptional WithBlue(byte blue);
        IGlowOptional WithAlpha(byte alpha);
        IGlowOptional WithDelay(int delay);
        IGlowOptional WithDuration(int duration);
    }
}