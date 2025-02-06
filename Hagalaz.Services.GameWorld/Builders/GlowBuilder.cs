using Hagalaz.Game.Abstractions.Builders.Glow;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Services.GameWorld.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class GlowBuilder : IGlowBuilder, IGlowBuild, IGlowOptional
    {
        private byte _red;
        private byte _green;
        private byte _blue;
        private byte _alpha = 255;
        private int _duration = short.MaxValue;
        private int _delay;

        public IGlowOptional Create() => new GlowBuilder();

        public IGlow Build() => new Glow
        {
            Red = _red,
            Green = _green,
            Blue = _blue,
            Alpha = _alpha,
            Duration = _duration,
            Delay = _delay
        };

        public IGlowOptional WithAlpha(byte alpha)
        {
            _alpha = alpha;
            return this;
        }

        public IGlowOptional WithDelay(int delay)
        {
            _delay = delay;
            return this;
        }

        public IGlowOptional WithDuration(int duration)
        {
            _duration = duration;
            return this;
        }

        public IGlowOptional WithBlue(byte blue)
        {
            _blue = blue;
            return this;
        }

        public IGlowOptional WithRed(byte red)
        {
            _red = red;
            return this;
        }

        public IGlowOptional WithGreen(byte green)
        {
            _green = green;
            return this;
        }
    }
}