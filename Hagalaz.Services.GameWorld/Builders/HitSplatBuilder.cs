using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class HitSplatSpriteBuilder : IHitSplatSpriteBuilder, IHitSplatSpriteOptional
    {
        public int? Damage { get; private set; }
        public HitSplatType? SplatType { get; private set; }
        public DamageType? DamageType { get; private set; }
        public int? MaxDamage { get; private set; }
        public bool? IsCriticalDamage { get; private set; }

        public IHitSplatSpriteOptional WithDamage(int damage)
        {
            Damage = damage;
            return this;
        }

        public IHitSplatSpriteOptional WithSplatType(HitSplatType type)
        {
            SplatType = type;
            return this;
        }

        public IHitSplatSpriteOptional WithDamageType(DamageType damageType)
        {
            DamageType = damageType;
            return this;
        }

        public IHitSplatSpriteOptional WithMaxDamage(int maxDamage)
        {
            MaxDamage = maxDamage;
            return this;
        }

        public IHitSplatSpriteOptional AsCriticalDamage()
        {
            IsCriticalDamage = true;
            return this;
        }
    }

    public class HitSplatBuilder : IHitSplatBuilder, IHitSplatOptional, IHitSplatBuild, IHitSplatSprite
    {
        private int? _delay;
        private IRuneObject? _sender;
        private readonly List<HitSplatSpriteBuilder> _sprites = [];

        public IHitSplatSprite Create() => new HitSplatBuilder();

        public IHitSplat Build()
        {
            var firstSplatDamage = _sprites[0].Damage;
            var firstSplatType = _sprites[0].SplatType;
            var firstSplatCritical = _sprites[0].IsCriticalDamage;
            var firstSplatDamageType = _sprites[0].DamageType;
            var firstSplatMaxDamage = _sprites[0].MaxDamage;
            var secondSplatDamage = _sprites[1]?.Damage;
            var secondSplatType = _sprites[1]?.SplatType;
            var secondSplatCritical = _sprites[1]?.IsCriticalDamage;

            if (firstSplatType is null && firstSplatDamageType is not null)
            {
                firstSplatType = firstSplatDamageType.Value.ToHitSplatType();
            }
            if (firstSplatCritical is null && firstSplatMaxDamage is not null)
            {
                firstSplatCritical = firstSplatDamage >= (firstSplatMaxDamage * 0.9);
            }
            return new HitSplat
            {
                Sender = _sender,
                Delay = _delay ?? 0,
                FirstSplatDamage = firstSplatDamage is null or -1 ? 0 : firstSplatDamage.Value,
                FirstSplatType = firstSplatDamage is null or -1 ? HitSplatType.HitMiss : firstSplatType ?? HitSplatType.None,
                FirstSplatCritical = firstSplatDamage != -1 && firstSplatCritical.HasValue && firstSplatCritical.Value,
                SecondSplatDamage = secondSplatDamage is null or -1 ? 0 : secondSplatDamage.Value,
                SecondSplatType = secondSplatType is null || secondSplatDamage is null or -1 ? HitSplatType.None : secondSplatType.Value,
                SecondSplatCritical = secondSplatCritical ?? false
            };
        }

        public IHitSplatOptional WithDelay(int delay)
        {
            _delay = delay;
            return this;
        }

        public IHitSplatOptional FromSender(IRuneObject sender)
        {
            _sender = sender;
            return this;
        }

        public IHitSplatSprite AddSprite(Action<IHitSplatSpriteBuilder> builder)
        {
            var spriteBuilder = new HitSplatSpriteBuilder();
            builder(spriteBuilder);
            _sprites.Add(spriteBuilder);
            return this;
        }
}
}