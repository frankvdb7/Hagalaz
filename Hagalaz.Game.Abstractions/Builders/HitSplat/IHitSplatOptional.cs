using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.HitSplat
{
    public interface IHitSplatOptional : IHitSplatBuild
    {
        IHitSplatOptional WithDelay(int delay);
        IHitSplatOptional FromSender(IRuneObject sender);
    }
}