using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Builders.Region;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Npcs.Elementals
{
    [NpcScriptMetaData([14301])]
    public class Glacor : NpcScriptBase
    {
        /// <summary>
        ///     The charge ticks.
        /// </summary>
        private const int _chargeTicks = 10;

        /// <summary>
        /// </summary>
        private enum AttackType
        {
            /// <summary>
            ///     The melee
            /// </summary>
            Melee,

            /// <summary>
            ///     The ranged
            /// </summary>
            Ranged,

            /// <summary>
            ///     The magic
            /// </summary>
            Magic,

            /// <summary>
            ///     The icicle
            /// </summary>
            Icicle
        }

        /// <summary>
        ///     The glacytes dead.
        /// </summary>
        private bool _glacytesDead;

        /// <summary>
        ///     The glacyte dead count.
        /// </summary>
        private int _glacyteDeadCount;

        /// <summary>
        ///     The last killed glacyte identifier.
        /// </summary>
        private int _lastKilledGlacyteId;

        /// <summary>
        ///     The minions spawned.
        /// </summary>
        private bool _glacytesSpawned;

        /// <summary>
        ///     The attack type.
        /// </summary>
        private AttackType _attackType;

        /// <summary>
        ///     The charge.
        /// </summary>
        private int _charge;

        /// <summary>
        ///     The healing.
        /// </summary>
        private bool _healing;

        /// <summary>
        ///     The glacytes
        /// </summary>
        private readonly List<INpcHandle> _glacytes = [];

        private readonly INpcBuilder _npcBuilder;
        private readonly IRegionUpdateBuilder _regionUpdateBuilder;
        private readonly IProjectileBuilder _projectileBuilder;
        private readonly IHitSplatBuilder _hitSplatBuilder;

        public Glacor(INpcBuilder npcBuilder, IRegionUpdateBuilder regionUpdateBuilder, IProjectileBuilder projectileBuilder, IHitSplatBuilder hitSplatBuilder)
        {
            _npcBuilder = npcBuilder;
            _regionUpdateBuilder = regionUpdateBuilder;
            _projectileBuilder = projectileBuilder;
            _hitSplatBuilder = hitSplatBuilder;
        }

        /// <summary>
        ///     Starts the charge.
        /// </summary>
        private void StartCharge() => _charge = 0;

        /// <summary>
        ///     Heals this instance.
        /// </summary>
        private void Heal()
        {
            if (!_healing)
            {
                return;
            }

            if (Owner.Statistics.LifePoints < Owner.Definition.MaxLifePoints / 2)
            {
                Owner.Statistics.HealLifePoints(10);
            }
            else
            {
                _healing = false;
                StartCharge();
            }
        }

        /// <summary>
        ///     Explodes this instance.
        /// </summary>
        private void Explode()
        {
            Owner.Movement.Lock(true);
            Owner.QueueGraphic(Graphic.Create(439));
            Owner.QueueAnimation(Animation.Create(9964));

            var npcDamage = (int)(Owner.Statistics.LifePoints * 0.9);
            Owner.Statistics.DamageLifePoints(npcDamage);
            var npcSplat = _hitSplatBuilder.Create()
                .AddSprite(builder => builder.WithDamage(npcDamage).AsCriticalDamage())
                .Build();
            Owner.QueueHitSplat(npcSplat);

            var characters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => c.WithinRange(Owner, 2));
            foreach (var character in characters)
            {
                Owner.Combat.PerformAttack(new AttackParams()
                {
                    Damage = character.Statistics.LifePoints / 3,
                    MaxDamage = character.Statistics.LifePoints / 3,
                    DamageType = DamageType.Standard,
                    Target = character
                });
            }

            _healing = true;
            Owner.QueueTask(new RsTask(() => { Owner.Movement.Unlock(true); }, 2));
        }

        /// <summary>
        ///     Called when [set target].
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnSetTarget(ICreature target)
        {
            foreach (var glacyte in _glacytes.Where(glacyte => glacyte.Npc.Combat.Target == null))
            {
                glacyte.Npc.QueueTask(new RsTask(() => glacyte.Npc.Combat.SetTarget(target), 1));
            }
        }

        /// <summary>
        ///     Get's if this npc can retaliate to specific character attack.
        ///     By default, this method returns true.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can retaliate to] the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanRetaliateTo(ICreature creature) => Owner.Combat.Target == null;

        /// <summary>
        ///     Get's if this npc can be attacked by the specified character ('attacker').
        ///     By default , this method does check if this npc is attackable.
        ///     This method also checks if the attacker is a character, wether or not it
        ///     has the required slayer level.
        /// </summary>
        /// <param name="attacker">Creature which is attacking this npc.</param>
        /// <returns>
        ///     If attack can be performed.
        /// </returns>
        public override bool CanBeAttackedBy(ICreature attacker)
        {
            if (Owner.Combat.Target == null || Owner.Combat.Target == attacker)
            {
                return base.CanBeAttackedBy(attacker);
            }

            (attacker as ICharacter)?.SendChatMessage("Someone else is already fighting that glacor.");

            return false;
        }

        /// <summary>
        ///     Spawns the glacytes.
        /// </summary>
        private void SpawnGlacytes()
        {
            if (_glacytesSpawned || Owner.Combat.Target == null)
            {
                return;
            }

            Owner.QueueAnimation(Animation.Create(9964));
            const byte delay = 50;
            var direction = Owner.FaceDirection;
            var deltaX = direction.GetDeltaX();
            var deltaY = direction.GetDeltaY();

            var from = Owner.Location.Translate(1, 1, 0);
            var center = Owner.Location.Translate(deltaX, deltaY, 0);
            var left = center.Translate(1, 0, 0);
            var right = center.Translate(-1, 0, 0);

            _projectileBuilder.Create()
                .WithGraphicId(2875)
                .FromLocation(from)
                .ToLocation(center)
                .WithDuration(delay)
                .WithFromHeight(25)
                .WithToHeight(15)
                .Send();

            _projectileBuilder.Create()
                .WithGraphicId(2875)
                .FromLocation(from)
                .ToLocation(left)
                .WithDuration(delay)
                .WithFromHeight(25)
                .WithToHeight(15)
                .Send();

            _projectileBuilder.Create()
                .WithGraphicId(2875)
                .FromLocation(from)
                .ToLocation(right)
                .WithDuration(delay)
                .WithFromHeight(25)
                .WithToHeight(15)
                .Send();

            Owner.QueueTask(new RsTask(() =>
                {
                    RegisterGlacyte(14304, center, new EnduringGlacyte(Owner));
                    RegisterGlacyte(14303, left, new SappingGlacyte(Owner));
                    RegisterGlacyte(14302, right, new UnstableGlacyte(Owner, _hitSplatBuilder));
                },
                CreatureHelper.CalculateTicksForClientTicks(delay)));

            _glacytesSpawned = true;
            _glacyteDeadCount = 0;
        }

        private void RegisterGlacyte(int id, ILocation location, INpcScript script)
        {
            var handle = _npcBuilder.Create()
                .WithId(id)
                .WithLocation(location)
                .WithScript(script)
                .Spawn();
            var glacyte = handle.Npc;
            glacyte.QueueTask(new RsTask(() => glacyte.Combat.SetTarget(Owner.Combat.Target), 1));

            EventHappened happ = null;
            happ = glacyte.RegisterEventHandler(new EventHappened<CreatureDiedEvent>(e =>
            {
                _glacyteDeadCount++;
                _glacytesDead = _glacyteDeadCount >= 3;
                _lastKilledGlacyteId = glacyte.Appearance.CompositeID;
                if (_glacytesDead)
                {
                    if (_lastKilledGlacyteId == 14302)
                    {
                        StartCharge();
                    }

                    var deltaX = Owner.Location.X - glacyte.Location.X;
                    var deltaY = Owner.Location.Y - glacyte.Location.Y;
                    if (deltaX < 0)
                    {
                        deltaX = -deltaX;
                    }

                    if (deltaY < 0)
                    {
                        deltaY = -deltaY;
                    }

                    var delay = 20 + deltaX * 5 + deltaY * 5;

                    _projectileBuilder.Create()
                        .WithGraphicId(2875)
                        .FromCreature(glacyte)
                        .ToCreature(Owner)
                        .WithDuration(delay)
                        .WithFromHeight(15)
                        .WithToHeight(25)
                        .Send();
                }

                _glacytes.Remove(handle);
                glacyte.UnregisterEventHandler<CreatureDiedEvent>(happ);
                return false;
            }));

            _glacytes.Add(handle);
        }

        public override int OnIncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay)
        {
            if (_glacytesSpawned && !_glacytesDead)
            {
                return 0;
            }

            if (_glacytesDead && _lastKilledGlacyteId == 14304)
            {
                return (int)(damage * 0.40);
            }

            return base.OnIncomingAttack(attacker, damageType, damage, delay);
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType()
        {
            switch (_attackType)
            {
                case AttackType.Melee: return AttackBonus.Crush;
                case AttackType.Magic: return AttackBonus.Magic;
                case AttackType.Ranged:
                case AttackType.Icicle:
                    return AttackBonus.Ranged;
                default: return base.GetAttackBonusType();
            }
        }

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle()
        {
            switch (_attackType)
            {
                case AttackType.Melee: return AttackStyle.MeleeAggressive;
                case AttackType.Magic: return AttackStyle.MagicNormal;
                case AttackType.Ranged:
                case AttackType.Icicle:
                    return AttackStyle.RangedAccurate;
                default: return base.GetAttackStyle();
            }
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance() => 7;

        /// <summary>
        ///     Get's called when npc is dead.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnDeath()
        {
            _glacytesDead = _glacytesSpawned = false;
            _glacyteDeadCount = _lastKilledGlacyteId = 0;
            _glacytes.Clear();
        }

        /// <summary>
        ///     Get's attack speed of this npc.
        ///     By default, this method does return Definition.AttackSpeed.
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackSpeed()
        {
            if (_glacytesDead)
            {
                return Owner.Definition.AttackSpeed - 1;
            }

            return Owner.Definition.AttackSpeed;
        }

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            GenerateAttackType(target);
            switch (_attackType)
            {
                case AttackType.Ranged:
                    {
                        Owner.QueueAnimation(Animation.Create(9968));

                        var combat = (INpcCombat)Owner.Combat;

                        var deltaX = Owner.Location.X - target.Location.X;
                        var deltaY = Owner.Location.Y - target.Location.Y;
                        if (deltaX < 0)
                        {
                            deltaX = -deltaX;
                        }

                        if (deltaY < 0)
                        {
                            deltaY = -deltaY;
                        }

                        var duration = 20 + deltaX * 5 + deltaY * 5;

                        _projectileBuilder.Create()
                            .WithGraphicId(962)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(duration)
                            .WithDelay(10)
                            .WithFromHeight(50)
                            .WithToHeight(35)
                            .WithSlope(1)
                            .Send();

                        Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = combat.GetRangeDamage(target),
                            MaxDamage = combat.GetRangeMaxHit(target),
                            DamageType = DamageType.FullRange,
                            Delay = duration,
                            Target = target
                        });
                        break;
                    }
                case AttackType.Magic:
                    {
                        Owner.QueueAnimation(Animation.Create(9967));
                        Owner.QueueGraphic(Graphic.Create(902));

                        var combat = (INpcCombat)Owner.Combat;

                        var deltaX = Owner.Location.X - target.Location.X;
                        var deltaY = Owner.Location.Y - target.Location.Y;
                        if (deltaX < 0)
                        {
                            deltaX = -deltaX;
                        }

                        if (deltaY < 0)
                        {
                            deltaY = -deltaY;
                        }

                        var duration = 20 + deltaX * 5 + deltaY * 5;

                        _projectileBuilder.Create()
                            .WithGraphicId(2875)
                            .FromCreature(Owner)
                            .ToCreature(target)
                            .WithDuration(duration)
                            .WithDelay(10)
                            .WithFromHeight(25)
                            .WithToHeight(15)
                            .WithSlope(0)
                            .Send();

                        var handle = Owner.Combat.PerformAttack(new AttackParams()
                        {
                            Damage = combat.GetMagicDamage(target, 300),
                            MaxDamage = combat.GetMagicMaxHit(target, 300),
                            DamageType = DamageType.FullMagic,
                            Delay = duration,
                            Target = target
                        });

                        handle.RegisterResultHandler(result =>
                        {
                            if (RandomStatic.Generator.Next(0, 15) == 10)
                            {
                                if (((ICharacter)target).Prayers.IsPraying(NormalPrayer.ProtectFromMagic) ||
                                    ((ICharacter)target).Prayers.IsPraying(AncientCurses.DeflectMagic))
                                {
                                    return;
                                }

                                target.Freeze(5, 15);
                            }
                        });
                        break;
                    }
                case AttackType.Icicle:
                    {
                        var targetLocation = target.Location.Clone();
                        Owner.QueueAnimation(Animation.Create(9955));

                        var deltaX = Owner.Location.X - targetLocation.X;
                        var deltaY = Owner.Location.Y - targetLocation.Y;
                        if (deltaX < 0)
                        {
                            deltaX = -deltaX;
                        }

                        if (deltaY < 0)
                        {
                            deltaY = -deltaY;
                        }

                        var duration = 50 + deltaX * 5 + deltaY * 5;

                        _projectileBuilder.Create()
                            .WithGraphicId(963)
                            .FromLocation(Owner.Location.Translate(1, 1, 0))
                            .ToLocation(targetLocation)
                            .WithDuration(duration)
                            .WithDelay(10)
                            .WithFromHeight(50)
                            .WithToHeight(35)
                            .WithSlope(1)
                            .Send();

                        _regionUpdateBuilder.Create()
                            .WithLocation(targetLocation)
                            .WithGraphic(Graphic.Create(899, duration))
                            .Queue();


                        Owner.QueueTask(new RsTask(() =>
                            {
                                var characters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => c.Location.Equals(targetLocation));
                                foreach (var character in characters)
                                {
                                    Owner.Combat.PerformAttack(new AttackParams()
                                    {
                                        Damage = character.Statistics.LifePoints / 2,
                                        MaxDamage = character.Statistics.LifePoints / 2,
                                        DamageType = DamageType.Standard,
                                        Target = character
                                    });
                                }
                            },
                            CreatureHelper.CalculateTicksForClientTicks(duration)));
                        break;
                    }
                default:
                    {
                        base.PerformAttack(target);
                        if (RandomStatic.Generator.Next(0, 25) == 10)
                        {
                            target.AddState(new State(StateType.GlacorFrozen, int.MaxValue));
                        }

                        break;
                    }
            }

            if (!_glacytesDead || _lastKilledGlacyteId != 14303)
            {
                return;
            }

            (target as ICharacter)?.Statistics.DrainPrayerPoints(50);
        }

        /// <summary>
        ///     Generates the type of the attack.
        /// </summary>
        private void GenerateAttackType(ICreature? target)
        {
            if (_attackType == AttackType.Icicle)
            {
                _attackType = AttackType.Ranged;
                return;
            }

            var chances = new double[4];
            chances[0] = 0.25; // magic
            chances[1] = 0.25; // ranged
            chances[2] = 0.10; // icicle
            chances[3] = 0.0; // melee
            if (target != null && Owner.WithinRange(target, 1))
            {
                chances[3] += 0.75;
            }

            var hitValue = Random.Shared.Next(chances.Sum());
            var runningValue = 0.0;
            for (var i = 0; i < chances.Length; i++)
            {
                runningValue += chances[i];
                if (!(hitValue < runningValue))
                {
                    continue;
                }

                switch (i)
                {
                    case 0:
                        _attackType = AttackType.Magic;
                        return;
                    case 1:
                        _attackType = AttackType.Ranged;
                        return;
                    case 2:
                        _attackType = AttackType.Icicle;
                        return;
                    case 3: _attackType = AttackType.Melee; break;
                }

                break;
            }
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() => GenerateAttackType(null);

        /// <summary>
        ///     Tick's npc.
        ///     By default, this method does nothing.
        /// </summary>
        public override void Tick()
        {
            if (Owner.Combat.IsDead)
            {
                return;
            }

            if (!_glacytesSpawned && Owner.Statistics.LifePoints < Owner.Definition.MaxLifePoints / 2)
            {
                SpawnGlacytes();
            }

            if (!_glacytesDead || _lastKilledGlacyteId != 14302)
            {
                return;
            }

            if (_healing)
            {
                Heal();
            }
            else if (++_charge >= _chargeTicks)
            {
                Explode();
            }
        }
    }
}