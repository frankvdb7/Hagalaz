using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    ///     Abstract class for character combat.
    /// </summary>
    public abstract class CreatureCombat : ICreatureCombat
    {
        /// <summary>
        ///     Owner of this combat.
        /// </summary>
        protected readonly ICreature Owner;

        /// <summary>
        ///     Contains all attackers.
        /// </summary>
        private readonly List<ICreatureAttackerInfo> _recentAttackers = [];

        /// <summary>
        ///     The killers.
        /// </summary>
        private readonly List<ICreatureAttackerInfo> _attackers = [];

        /// <summary>
        /// The projectile path finder
        /// </summary>
        private readonly IProjectilePathFinder _projectilePathFinder;

        /// <summary>
        /// The smart path finder
        /// </summary>
        private readonly ISmartPathFinder _smartPathFinder;

        /// <summary>
        /// 
        /// </summary>
        private readonly IOptions<CombatOptions> _combatOptions;

        /// <summary>
        ///     Contains combat delay tick.
        /// </summary>
        /// <value>The delay tick.</value>
        public int DelayTick { get; protected set; }

        /// <summary>
        ///     Contains boolean if specific character is dead.
        /// </summary>
        /// <value><c>true</c> if this instance is dead; otherwise, <c>false</c>.</value>
        public bool IsDead { get; protected set; }

        /// <summary>
        ///     Contains target character or null.
        /// </summary>
        /// <value>The target.</value>
        public ICreature? Target { get; protected set; }

        /// <summary>
        ///     Contains last target or null.
        /// </summary>
        /// <value>The last attacked.</value>
        public ICreature? LastAttacked { get; protected set; }

        /// <summary>
        ///     Contains the recent attackers count.
        /// </summary>
        public int RecentAttackersCount => _recentAttackers.Count;

        /// <summary>
        /// Gets the recent attackers.
        /// </summary>
        /// <value>
        /// The recent attackers.
        /// </value>
        public IEnumerable<ICreatureAttackerInfo> RecentAttackers => _recentAttackers.AsEnumerable();

        /// <summary>
        ///     Construct's new combat class.
        /// </summary>
        /// <param name="owner">Owner of this class.</param>
        protected CreatureCombat(ICreature owner)
        {
            Owner = owner;
            DelayTick = 17;

            _projectilePathFinder = owner.ServiceProvider.GetRequiredService<IProjectilePathFinder>();
            _smartPathFinder = owner.ServiceProvider.GetRequiredService<ISmartPathFinder>();
            _combatOptions = owner.ServiceProvider.GetRequiredService<IOptions<CombatOptions>>();
        }

        /// <summary>
        ///     Render's death of this creature.
        /// </summary>
        public abstract void OnDeath();

        /// <summary>
        ///     This method gets executed when creature kills the target.
        /// </summary>
        /// <param name="target">The target.</param>
        public abstract void OnTargetKilled(ICreature target);

        /// <summary>
        ///     This method gets executed on creatures death by creature.
        /// </summary>
        /// <param name="killer">The creature.</param>
        public abstract void OnKilledBy(ICreature killer);

        /// <summary>
        ///     Determines whether this instance [can be looted].
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns><c>true</c> if this instance [can be looted]; otherwise, <c>false</c>.</returns>
        public abstract bool CanBeLootedBy(ICreature killer);

        /// <summary>
        ///     Render's spawning of this character.
        /// </summary>
        public abstract void OnSpawn();

        /// <summary>
        ///     Tick's curses effects.
        /// </summary>
        protected abstract void CursesTick();

        /// <summary>
        ///     Get's if this character can attack specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns><c>true</c> if this instance can attack the specified target; otherwise, <c>false</c>.</returns>
        public abstract bool CanAttack(ICreature target);

        /// <summary>
        ///     Get's if this creature can be attacked by specified attacker.
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <returns><c>true</c> if this instance [can be attacked by] the specified attacker; otherwise, <c>false</c>.</returns>
        public abstract bool CanBeAttackedBy(ICreature attacker);

        /// <summary>
        ///     Set's ( Attacks ) the specified target ('target')
        /// </summary>
        /// <param name="target">Creature which should be attacked.</param>
        /// <returns>If creature target was set sucessfully.</returns>
        public abstract bool SetTarget(ICreature target);

        /// <summary>
        ///     Determines whether this instance [can set target] the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns><c>true</c> if this instance [can set target] the specified target; otherwise, <c>false</c>.</returns>
        public abstract bool CanSetTarget(ICreature target);

        /// <summary>
        ///     Get's called after attack was performed to specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public abstract void OnAttackPerformed(ICreature target);

        /// <summary>
        ///     Cancel's current target.
        /// </summary>
        public abstract void CancelTarget();

        /// <summary>
        ///     Get's called when last attacked get's null.
        /// </summary>
        protected abstract void OnLastAttackedFade();

        /// <summary>
        ///     Get's character attack range.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public abstract int GetAttackDistance();

        /// <summary>
        ///     Get's attack speed of the character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public abstract int GetAttackSpeed();

        /// <summary>
        ///     Get's attack style of this character.
        /// </summary>
        /// <returns>AttackStyle.</returns>
        public abstract AttackStyle GetAttackStyle();

        /// <summary>
        ///     Get's attakc bonus of this character.
        /// </summary>
        /// <returns>AttackBonus.</returns>
        public abstract AttackBonus GetAttackBonusType();

        /// <summary>
        ///     Get's specific bonus for this character.
        /// </summary>
        /// <param name="bonusType">Type of the bonus.</param>
        /// <returns>System.Int32.</returns>
        public abstract int GetBonus(BonusType bonusType);

        /// <summary>
        ///     Get's specific prayer bonus for this character.
        /// </summary>
        /// <param name="bonusType">Type of the bonus.</param>
        /// <returns>System.Int32.</returns>
        public abstract int GetPrayerBonus(BonusPrayerType bonusType);

        /// <summary>
        ///     Get's attack level of this character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public abstract int GetAttackLevel();

        /// <summary>
        ///     Get's strength level of this character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public abstract int GetStrengthLevel();

        /// <summary>
        ///     Get's defence level of this character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public abstract int GetDefenceLevel();

        /// <summary>
        ///     Get's ranged level.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public abstract int GetRangedLevel();

        /// <summary>
        ///     Get's magic level .
        /// </summary>
        /// <returns>System.Int32.</returns>
        public abstract int GetMagicLevel();

        /// <summary>
        ///     Gets the last attack tick.
        /// </summary>
        /// <returns></returns>
        public int GetLastAttackerTick() => _attackers.Select(attacker => attacker.LastAttackTick).Prepend(-1).Max();

        /// <summary>
        ///     Perform's incomming attack on this character.
        ///     This attack does not damage the target but does check for protection prayers and performs
        ///     animations.
        ///     Attacker target must be this character.
        ///     If return is -1 , it means that attack wasn't performed.
        /// </summary>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage, can be -1 incase of miss.</param>
        /// <param name="delay">Delay in client ticks until the attack will reach target.</param>
        /// <returns>Returns amount of damage that should be dealed.</returns>
        public abstract int IncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay);

        /// <summary>
        ///     Perform's attack on this character.
        ///     Attacker target must be this character.
        ///     Returns amount of damage that should be rendered on hitsplat.
        ///     If return is -1 , it means that attack wasn't performed.
        /// </summary>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage, can be -1 incase of miss.</param>
        /// <param name="soaked">
        ///     Variable which will be set to amount of damage that was soaked.
        ///     If no damage was soaked then the variable will be -1.
        /// </param>
        /// <returns>Returns amount of damage that should be rendered on hitsplat.</returns>
        public abstract int Attack(ICreature attacker, DamageType damageType, int damage, ref int soaked);

        /// <summary>
        ///     Performs the attack.
        /// </summary>
        protected abstract void AttackTick();

        /// <summary>
        ///     Render's creature death.
        /// </summary>
        /// <value>
        ///     How long the death animation takes.
        /// </value>
        protected abstract int RenderDeath();

        public virtual IRsTaskHandle<AttackResult> PerformAttack(AttackParams attackParams)
        {
            var distance = Owner.Location.GetDistance(attackParams.Target.Location);
            var delay = attackParams.Delay + (int)Math.Round(Math.Max(0, distance - 1) * 2);

            var hitSplatBuilder = Owner.ServiceProvider.GetRequiredService<IHitSplatBuilder>();
            var incomingDamage = attackParams.Target.Combat.IncomingAttack(Owner, attackParams.DamageType, attackParams.Damage, delay);
            return attackParams.Target.QueueTask(new RsTask<AttackResult>(() =>
            {
                var soak = -1;
                var damage = attackParams.Target.Combat.Attack(Owner, attackParams.DamageType, incomingDamage, ref soak);
                var splat = hitSplatBuilder.Create()
                    .AddSprite(builder => builder
                        .WithDamage(damage)
                        .WithMaxDamage(attackParams.MaxDamage)
                        .WithDamageType(attackParams.DamageType))
                    .AddSprite(builder => builder
                        .WithDamage(soak)
                        .WithSplatType(HitSplatType.HitDefendedDamage))
                    .FromSender(Owner)
                    .Build();
                attackParams.Target.QueueHitSplat(splat);

                return new AttackResult
                {
                    Damage = (incomingDamage != -1, incomingDamage),
                    DamageLifePoints = (damage != -1, damage)
                };
            }, CreatureHelper.CalculateTicksForClientTicks(delay)));
        }

        /// <summary>
        ///     Add's specific character to attackers list.
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        protected void AddAttacker(ICreature attacker)
        {
            var attackerRef = _recentAttackers.FirstOrDefault(att => att.Attacker == attacker);
            if (attackerRef != null)
            {
                attackerRef.LastAttackTick = 0;
                return;
            }

            var attack = new CreatureAttackerInfo(attacker, 0);
            _recentAttackers.Add(attack);

            for (var index = 0; index < _attackers.Count; index++)
                if (_attackers[index].Attacker == attacker)
                {
                    _attackers[index] = attack;
                    return;
                }

            _attackers.Add(attack);
        }

        /// <summary>
        ///     Gets the killer.
        /// </summary>
        /// <returns>Creature.</returns>
        public ICreature? GetKiller()
        {
            ICreature? killer = null;
            var damage = -1;
            foreach (var att in _attackers.Where(att => att.TotalDamage > damage))
            {
                killer = att.Attacker;
                damage = att.TotalDamage;
            }

            if (killer is not INpc npc)
            {
                return killer;
            }

            if (npc.TryGetScript<IFamiliarScript>(out var script))
            {
                killer = script.Summoner;
            }

            return killer;
        }

        /// <summary>
        ///     Adds the damage to attacker.
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <param name="damage">The damage.</param>
        protected void AddDamageToAttacker(ICreature attacker, int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            var refAttack = _recentAttackers.FirstOrDefault(att => att.Attacker == attacker);
            if (refAttack != null)
            {
                refAttack.TotalDamage += damage;
            }
        }

        /// <summary>
        ///     Reaches the target.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <returns>
        ///     Returns true if the target can be reached.
        /// </returns>
        private bool ReachTarget(int range)
        {
            if (Target == null)
            {
                return false;
            }

            if (Owner.Location.Z != Target.Location.Z || Owner.Location.Dimension != Target.Location.Dimension)
            {
                return false;
            }


            // TODO - fix bug when target is 1 tick ahead
            Owner.FaceCreature(Target);

            var thisSize = Owner.Size;
            var otherSize = Target.Size;
            int myX = Owner.Location.X;
            int myY = Owner.Location.Y;
            int otherX = Target.Movement.Moved && !Target.Movement.Teleported && Target.LastLocation != null
                ? Target.LastLocation.X
                : Target.Location.X;
            int otherY = Target.Movement.Moved && !Target.Movement.Teleported && Target.LastLocation != null
                ? Target.LastLocation.Y
                : Target.Location.Y;

            var onSameTile = false;
            var withinRange = false;
            for (var x1 = 0; x1 < thisSize; x1++)
            {
                for (var y1 = 0; y1 < thisSize; y1++)
                {
                    for (var x2 = 0; x2 < otherSize; x2++)
                    {
                        for (var y2 = 0; y2 < otherSize; y2++)
                        {
                            var distance = (int)Location.GetDistance(myX + x1, myY + y1, otherX + x2, otherY + y2);
                            if (distance <= 0)
                                onSameTile = true;
                            else if (distance <= range) withinRange = true;
                        }
                    }
                }
            }

            if (Owner.Movement.Locked && !withinRange) return false;

            if (onSameTile)
            {
                if (Target.Movement.Moving)
                {
                    return false;
                }

                var adjacent = _smartPathFinder.FindAdjacent(Target);
                if (adjacent == null)
                {
                    return false;
                }

                Owner.Movement.AddToQueue(adjacent);
                return true;
            }

            if (range > 1 && withinRange)
            {
                Owner.Movement.ClearQueue();
                if (!_projectilePathFinder.Find(Owner, Target, true).Successful) withinRange = false;
            }

            if (!withinRange)
            {
                var path = Owner.PathFinder.Find(Owner, Target, true);
                if (!path.Successful && !path.MovedNear || path.MovedNearDestination)
                {
                    if (Owner is ICharacter character)
                    {
                        character.SendChatMessage(GameStrings.YouCantReachThat);
                        CancelTarget();
                    }

                    return false;
                }

                if (path.ReachedDestination) return true;
                Owner.Movement.AddToQueue(path);
            }

            return withinRange;
        }

        /// <summary>
        ///     Get's if this creature is in combat.
        /// </summary>
        /// <returns><c>true</c> if [is in combat]; otherwise, <c>false</c>.</returns>
        public bool IsInCombat()
        {
            if (Target != null) return true;
            return _recentAttackers.Count > 0;
        }

        /// <summary>
        ///     Tick's creatures's combat.
        /// </summary>
        public void Tick()
        {
            ConditionsTick();
            CursesTick();

            if (Target != null && ReachTarget(GetAttackDistance()) && DelayTick >= GetAttackSpeed())
            {
                AttackTick();
            }
        }

        /// <summary>
        ///     Perform's conditions tick.
        ///     Increases delaytick by one.
        ///     Removes target if SetTarget(this.Target) return's true
        ///     and cleans attackers that attack time is too old.
        ///     This method MUST be called by all combat implementations at the Tick()
        ///     method.
        /// </summary>
        private void ConditionsTick()
        {
            if (Target != null && !CanSetTarget(Target)) CancelTarget();

            DelayTick++;
            if (LastAttacked != null && LastAttacked.IsDestroyed)
            {
                OnLastAttackedFade();
                LastAttacked = null;
            }
            else if (LastAttacked != null)
            {
                var attackers = LastAttacked.Combat.RecentAttackers;
                var foundSelf = attackers.Any(attacker => attacker.Attacker == Owner);

                if (!foundSelf)
                {
                    OnLastAttackedFade();
                    LastAttacked = null;
                }
            }

            var att = new List<ICreatureAttackerInfo>(_recentAttackers);
            foreach (var info in att)
                if (info.Attacker is ICharacter)
                {
                    if (info.LastAttackTick > _combatOptions.Value.CharacterAttackTickDelay) _recentAttackers.Remove(info);
                }
                else if (info.Attacker is INpc)
                {
                    if (info.LastAttackTick > _combatOptions.Value.NpcAttackTickDelay) _recentAttackers.Remove(info);
                }

            var ks = new List<ICreatureAttackerInfo>(_attackers);
            foreach (var attacker in ks)
            {
                if (attacker.Attacker.IsDestroyed)
                {
                    _attackers.Remove(attacker);
                    continue;
                }

                if (Owner is ICharacter)
                    if (++attacker.LastAttackTick >= 500) // 5 minutes, then the attacker that dealt damage will be removed.
                        _attackers.Remove(attacker);
            }
        }

        /// <summary>
        ///     Clears the attackers.
        /// </summary>
        protected void ResetAttackers()
        {
            _recentAttackers.Clear();
            _attackers.Clear();
        }

        /// <summary>
        ///     Resets the combat delay.
        /// </summary>
        public void ResetCombatDelay() => DelayTick = 0;

        /// <summary>
        ///     Get's current attack bonus.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetAttackBonus()
        {
            switch (GetAttackBonusType())
            {
                case AttackBonus.Crush: return GetBonus(BonusType.AttackCrush);
                case AttackBonus.Slash: return GetBonus(BonusType.AttackSlash);
                case AttackBonus.Stab: return GetBonus(BonusType.AttackStab);
                case AttackBonus.Ranged: return GetBonus(BonusType.AttackRanged);
                case AttackBonus.Magic: return GetBonus(BonusType.AttackMagic);
                case AttackBonus.Summoning: return GetBonus(BonusType.AttackCrush);
            }

            return 0;
        }

        /// <summary>
        ///     Get's this character defence bonus.
        /// </summary>
        /// <param name="attackBonusType">Type of the attackers attack bonus.</param>
        /// <returns>System.Int32.</returns>
        public int GetDefenceBonus(AttackBonus attackBonusType)
        {
            switch (attackBonusType)
            {
                case AttackBonus.Crush: return GetBonus(BonusType.DefenceCrush);
                case AttackBonus.Slash: return GetBonus(BonusType.DefenceSlash);
                case AttackBonus.Stab: return GetBonus(BonusType.DefenceStab);
                case AttackBonus.Ranged: return GetBonus(BonusType.DefenceRanged);
                case AttackBonus.Magic: return GetBonus(BonusType.DefenceMagic);
                case AttackBonus.Summoning: return GetBonus(BonusType.DefenceSummoning);
            }

            return 0;
        }
    }
}