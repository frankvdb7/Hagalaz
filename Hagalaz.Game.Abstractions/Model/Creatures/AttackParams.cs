using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    public record AttackParams
    {
        public required ICreature Target { get; init; }
        public required DamageType DamageType { get; init; }
        public HitSplatType? HitType { get; init; }
        public required int Damage { get; init; }
        public required int MaxDamage { get; init; }
        public int Delay { get; init; }
    }
}