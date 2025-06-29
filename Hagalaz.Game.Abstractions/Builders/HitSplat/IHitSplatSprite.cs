using System;

namespace Hagalaz.Game.Abstractions.Builders.HitSplat
{
    public interface IHitSplatSprite
    {
        IHitSplatOptional AddSprite(Action<IHitSplatSpriteBuilder> builder);
    }
}