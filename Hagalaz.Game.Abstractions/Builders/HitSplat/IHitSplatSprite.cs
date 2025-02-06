using System;

namespace Hagalaz.Game.Abstractions.Builders.HitSplat
{
    public interface IHitSplatSprite : IHitSplatOptional
    {
        IHitSplatSprite AddSprite(Action<IHitSplatSpriteBuilder> builder);
    }
}