using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Builders.HitSplat
{
    public interface IHitSplatSpriteOptional
    {
        IHitSplatSpriteOptional WithSplatType(HitSplatType type);
        IHitSplatSpriteOptional WithDamageType(DamageType damageType);
        IHitSplatSpriteOptional WithMaxDamage(int maxDamage);
        IHitSplatSpriteOptional AsCriticalDamage();
    }
}