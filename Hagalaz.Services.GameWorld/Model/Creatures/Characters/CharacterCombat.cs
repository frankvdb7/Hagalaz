using System;
using System.Linq;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Model;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Combat implementation for character.
    /// </summary>
    public class CharacterCombat : CreatureCombat, ICharacterCombat
    {
        /// <summary>
        /// The character.
        /// </summary>
        private readonly ICharacter _character;

        private readonly IAnimationBuilder _animationBuilder;
        private readonly IGraphicBuilder _graphicBuilder;
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        /// Construct's new combat class for specified
        /// character.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public CharacterCombat(ICharacter owner)
            : base(owner)
        {
            _character = owner;
            _animationBuilder = _character.ServiceProvider.GetRequiredService<IAnimationBuilder>();
            _graphicBuilder = _character.ServiceProvider.GetRequiredService<IGraphicBuilder>();
            _projectileBuilder = _character.ServiceProvider.GetRequiredService<IProjectileBuilder>();
        }


        /// <summary>
        /// Render's character spawn.
        /// </summary>
        public override void OnSpawn() => IsDead = false;

        /// <summary>
        /// Render's character death.
        /// </summary>
        public override void OnDeath()
        {
            IsDead = true;
            var delay = RenderDeath();
            Owner.QueueTask(new RsTask(() =>
                {
                    var killer = GetKiller();
                    if (killer != null)
                    {
                        Owner.OnKilledBy(killer);
                        killer.OnTargetKilled(Owner);
                    }

                    ResetAttackers();
                    Owner.Respawn();
                },
                delay));

            _character.SendChatMessage("Oh dear, you have died.");
        }

        /// <summary>
        /// This method gets executed when creature kills the target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnTargetKilled(ICreature target) { }

        /// <summary>
        /// This method gets executed on creatures death.
        /// </summary>
        /// <param name="killer">The killer.</param>
        public override void OnKilledBy(ICreature killer)
        {
            if (!CanBeLootedBy(killer))
            {
                return;
            }

            ICharacter? characterKiller = null;
            if (killer is ICharacter character)
            {
                characterKiller = character;
            }

            characterKiller?.SendChatMessage(string.Format(
                GameStrings.ResourceManager.GetString(GameStrings.Combat_VictorKillMessage + RandomStatic.Generator.Next(9)),
                _character.DisplayName));
            // TODO
            //var database = ServiceLocator.Current.GetInstance<ISqlDatabaseManager>();
            //database.ExecuteAsync(new ActivityLogQuery(characterKiller.MasterId, "Player Kill", "I have defeated " + _character.DisplayName + " in combat."));

            var itemsOnDeath = _character.GetItemsOnDeathData();
            foreach (var equipment in _character.Equipment)
            {
                equipment?.EquipmentScript.OnUnequiped(equipment, _character); // make sure that any effects 'onequip', are removed by 'unequiping' them.
            }

            _character.Inventory.Clear(false);
            _character.Equipment.Clear(false);

            var groundItemOwner = _character;
            if (characterKiller != null)
            {
                groundItemOwner = characterKiller;
            }

            var groundItemBuilder = groundItemOwner.ServiceProvider.GetRequiredService<IGroundItemBuilder>();
            foreach (var item in
                     itemsOnDeath.droppedItems.Where(item => item.ItemScript.CanTradeItem(item, _character))) // check the current item owner if it can trade
            {
                var groundItem = groundItemBuilder.Create()
                    .WithItem(item)
                    .WithLocation(Owner.Location)
                    .WithOwner(groundItemOwner)
                    .Build();
                Owner.Region.Add(groundItem); // we spawn it with this method, as the container was normally stacked.
            }

            var bones = groundItemBuilder.Create()
                .WithItem(builder => builder.Create().WithId(526))
                .WithLocation(Owner.Location)
                .WithOwner(groundItemOwner)
                .Build();
            Owner.Region.Add(bones);
            _character.Inventory.AddRange(itemsOnDeath.keptItems);
            _character.Inventory.OnUpdate();
            _character.Equipment.OnUpdate();
        }

        /// <summary>
        /// Determines whether this instance [can be looted].
        /// </summary>
        /// <param name="killer">The killer, can be null.</param>
        /// <returns><c>true</c> if this instance [can be looted]; otherwise, <c>false</c>.</returns>
        public override bool CanBeLootedBy(ICreature killer) =>
            _character.Appearance.IsTransformedToNpc()
                ? _character.Appearance.PnpcScript.CanBeLootedBy(killer)
                : _character.GetScripts().All(script => script.CanBeLootedBy(killer));

        /// <summary>
        /// Render's character death.
        /// </summary>
        /// <returns>Amount of ticks the death gonna be rendered.</returns>
        protected override int RenderDeath()
        {
            if (_character.Appearance.IsTransformedToNpc()) return _character.Appearance.PnpcScript.RenderDeath();
            _character.QueueAnimation(_animationBuilder.Create().WithId(7197).Build());
            return 7;
        }

        /// <summary>
        /// Checks the skull conditions.
        /// </summary>
        /// <param name="target">The target.</param>
        public void CheckSkullConditions(ICreature target)
        {
            if (target.Combat.LastAttacked != null)
            {
                return;
            }

            var renderSkull = target switch
            {
                ICharacter => true,
                INpc npc => npc.HasScript<IFamiliarScript>(),
                _ => false
            };
            if (renderSkull) _character.RenderSkull(SkullIcon.DefaultSkull, 2000);
        }

        /// <summary>
        /// Set's ( Attacks ) the specified target ('target')
        /// </summary>
        /// <param name="target">Creature which should be attacked.</param>
        /// <returns>
        /// If creature target was set sucessfully.
        /// </returns>
        public override bool SetTarget(ICreature target)
        {
            if (!CanSetTarget(target)) return false;
            CheckSkullConditions(target);
            Target = target;
            Owner.FaceCreature(target);
            new CreatureSetCombatTargetEvent(Owner, target).Send();
            return true;
        }

        /// <summary>
        /// Determines whether this instance [can set target] the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns><c>true</c> if this instance [can set target] the specified target; otherwise, <c>false</c>.</returns>
        public override bool CanSetTarget(ICreature target)
        {
            if (target.IsDestroyed || target.Combat.IsDead || IsDead || !Owner.Viewport.VisibleCreatures.Contains(target)) return false;
            if (!target.Area.Script.CanBeAttacked(target, Owner)) return false;
            if (!Owner.Area.Script.CanAttack(Owner, target)) return false;
            if (!CanAttack(target)) return false;
            if (!target.Combat.CanBeAttackedBy(Owner)) return false;
            return true;
        }

        /// <summary>
        /// Cancel's current target.
        /// </summary>
        public override void CancelTarget()
        {
            _character.Magic.SelectedSpell = null;
            Target = null;
            Owner.ResetFacing();
        }

        /// <summary>
        /// Get's if this character can attack specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns><c>true</c> if this instance can attack the specified target; otherwise, <c>false</c>.</returns>
        public override bool CanAttack(ICreature target)
        {
            if (target.IsDestroyed || target.Combat.IsDead || IsDead || !Owner.Viewport.VisibleCreatures.Contains(target)) return false;
            if (!new AttackAllowEvent(_character, GetAttackStyle()).Send()) return false;
            var scripts = _character.GetScripts();
            return scripts.All(script => script.CanAttack(target));
        }

        /// <summary>
        /// Get's if this character can be attacked by specified attacker.
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <returns><c>true</c> if this instance [can be attacked by] the specified attacker; otherwise, <c>false</c>.</returns>
        public override bool CanBeAttackedBy(ICreature attacker)
        {
            if (attacker.IsDestroyed || attacker.Combat.IsDead || IsDead || !Owner.Viewport.VisibleCreatures.Contains(attacker)) return false;
            var scripts = _character.GetScripts();
            return scripts.All(script => script.CanBeAttackedBy(attacker));
        }

        /// <summary>
        /// Perform's preattack to specified target.
        /// This method is used by things such as soul split.
        /// </summary>
        /// <param name="target">Creature which should be hit.</param>
        /// <param name="predictedDamage">Total predicted damage.</param>
        public void PerformSoulSplit(ICreature target, int predictedDamage)
        {
            if (predictedDamage <= 0 || !_character.Prayers.IsPraying(AncientCurses.SoulSplit))
            {
                return;
            }

            var enemyPrayerDrain = (int)(Math.Round(predictedDamage * 0.10));
            var hpHeal = (int)(Math.Round(predictedDamage * 0.10));
            if (enemyPrayerDrain <= 0)
            {
                return;
            }

            var deltaX = Owner.Location.X - target.Location.X;
            var deltaY = Owner.Location.Y - target.Location.Y;
            if (deltaX < 0) deltaX = -deltaX;
            if (deltaY < 0) deltaY = -deltaY;

            var duration = Math.Max(30, deltaX * 15 + deltaY * 15);

            _projectileBuilder.Create()
                .WithGraphicId(2263)
                .FromCreature(Owner)
                .ToCreature(target)
                .WithDuration(duration)
                .WithDelay(30)
                .WithSlope(20)
                .WithFromHeight(11)
                .WithToHeight(11)
                .Send();

            _character.Statistics.HealLifePoints(hpHeal);
            if (target is ICharacter character)
            {
                character.Statistics.DrainPrayerPoints(enemyPrayerDrain);
            }

            Owner.QueueTask(new RsTask(() =>
                {
                    deltaX = Owner.Location.X - target.Location.X;
                    deltaY = Owner.Location.Y - target.Location.Y;
                    if (deltaX < 0) deltaX = -deltaX;
                    if (deltaY < 0) deltaY = -deltaY;
                    duration = Math.Max(30, deltaX * 15 + deltaY * 15);

                    target.QueueGraphic(_graphicBuilder.Create().WithId(2264).Build());
                    _projectileBuilder.Create()
                        .WithGraphicId(2263)
                        .FromCreature(target)
                        .ToCreature(Owner)
                        .WithDuration(duration)
                        .WithSlope(20)
                        .WithFromHeight(11)
                        .WithToHeight(11)
                        .Send();
                },
                CreatureHelper.CalculateTicksForClientTicks(duration + 30)));
        }

        /// <summary>
        /// Perform's incomming attack on this character.
        /// This attack does not damage the target but does check for protection prayers and performs
        /// animations.
        /// Attacker target must be this character.
        /// If return is -1 , it means that attack wasn't performed.
        /// </summary>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage, can be -1 in case of miss.</param>
        /// <param name="delay">Delay in client ticks until the attack will reach target.</param>
        /// <returns>Returns amount of damage that should be dealt.</returns>
        public override int IncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay)
        {
            if (IsDead) return -1;

            foreach (var item in _character.Equipment)
            {
                item?.EquipmentScript.OnIncomingAttack(item, _character, attacker, damageType, damage, delay);
            }

            var deflectType = 0;
            var protect = false;
            var protectFully = false;

            // the else ifs may cause bugs?
            if (damageType == DamageType.FullMelee || damageType == DamageType.StandardMelee)
            {
                if (Owner.HasState(StateType.MeleeImmunity))
                    protectFully = true;
                else if (_character.Prayers.IsPraying(AncientCurses.DeflectMelee) || _character.Prayers.IsPraying(NormalPrayer.ProtectFromMelee))
                {
                    protect = true;
                    deflectType = _character.Prayers.IsPraying(AncientCurses.DeflectMelee) ? 1 : 0;
                    protectFully = damageType == DamageType.StandardMelee;
                }
            }
            else if ((damageType == DamageType.FullRange || damageType == DamageType.StandardRange)
                     && _character.Prayers.IsPraying(AncientCurses.DeflectMissiles) || _character.Prayers.IsPraying(NormalPrayer.ProtectFromRanged))
            {
                protect = true;
                deflectType = _character.Prayers.IsPraying(AncientCurses.DeflectMissiles) ? 2 : 0;
                protectFully = damageType == DamageType.StandardRange;
            }
            else if ((damageType == DamageType.FullMagic || damageType == DamageType.StandardMagic)
                     && _character.Prayers.IsPraying(AncientCurses.DeflectMagic) || _character.Prayers.IsPraying(NormalPrayer.ProtectFromMagic))
            {
                protect = true;
                deflectType = _character.Prayers.IsPraying(AncientCurses.DeflectMagic) ? 3 : 0;
                protectFully = damageType == DamageType.StandardMagic;
            }
            else if ((damageType == DamageType.FullSummoning || damageType == DamageType.StandardSummoning)
                     && _character.Prayers.IsPraying(AncientCurses.DeflectSummoning) || _character.Prayers.IsPraying(NormalPrayer.ProtectFromSummoning))
            {
                protect = true;
                deflectType = _character.Prayers.IsPraying(AncientCurses.DeflectSummoning) ? 4 : 0; // TODO correct graphic id
                protectFully = damageType == DamageType.StandardSummoning;
            }

            if (deflectType > 0)
            {
                Owner.QueueAnimation(_animationBuilder.Create().WithId(12573).Build());
                Owner.QueueGraphic(_graphicBuilder.Create()
                    .WithId(deflectType switch
                    {
                        1 => 2230,
                        3 => 2228,
                        _ => 2229
                    })
                    .Build());

                Owner.Combat.PerformAttack(new AttackParams()
                {
                    Target = attacker, DamageType = DamageType.Reflected, Damage = damage, Delay = delay
                });
            }

            if (protectFully)
                damage = -1;
            else if (protect)
            {
                damage = (int)(damage * 0.6);
                if (damage <= 0) damage = -1;
            }

            switch (damageType)
            {
                case DamageType.Reflected: return damage;
                case DamageType.DragonFire:
                    {
                        string message;
                        if (Owner.HasState(StateType.SuperAntiDragonfirePotion) ||
                            Owner.HasState(StateType.AntiDragonfirePotion) && Owner.HasState(StateType.AntiDragonfireShield))
                        {
                            damage = -1;
                            message = GameStrings.DragonFireFully;
                        }
                        else if (_character.Prayers.IsPraying(AncientCurses.DeflectMagic) || _character.Prayers.IsPraying(NormalPrayer.ProtectFromMagic))
                        {
                            damage /= 5;
                            message = GameStrings.DragonFirePrayer;
                        }
                        else if (Owner.HasState(StateType.AntiDragonfirePotion) || Owner.HasState(StateType.AntiDragonfireShield))
                        {
                            damage /= 2;
                            message = GameStrings.DragonFireSome;
                        }
                        else
                        {
                            message = GameStrings.DragonFireBurned;
                        }

                        _character.SendChatMessage(message);
                        if (damage <= 0) damage = -1;
                        break;
                    }
            }

            if (_character.Appearance.IsTransformedToNpc())
                _character.Appearance.PnpcScript.RenderDefence(delay);
            else
            {
                var weapon = _character.Equipment[EquipmentSlot.Weapon];
                var shield = _character.Equipment[EquipmentSlot.Shield];
                if (shield != null)
                {
                    try
                    {
                        shield.EquipmentScript.RenderDefence(shield, _character, delay);
                        return damage;
                    }
                    catch (NotImplementedException) { }
                }

                if (weapon != null)
                {
                    try
                    {
                        weapon.EquipmentScript.RenderDefence(weapon, _character, delay);
                        return damage;
                    }
                    catch (NotImplementedException) { }
                }

                Owner.QueueAnimation(_animationBuilder.Create().WithId(424).WithDelay(delay).Build());
            }

            return damage;
        }

        /// <summary>
        /// Perform's attack on this character.
        /// Attacker target must be this character.
        /// Returns amount of damage that should be rendered on hitsplat.
        /// If return is -1 , it means that attack wasn't performed.
        /// </summary>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage, can be -1 in case of miss.</param>
        /// <param name="damageSoaked">Variable which will be set to amount of damage that was soaked.
        /// If no damage was soaked then the variable will be -1.</param>
        /// <returns>Returns amount of damage that should be rendered on hitsplat.</returns>
        public override int Attack(ICreature attacker, DamageType damageType, int damage, ref int damageSoaked)
        {
            if (IsDead) return -1;

            AddAttacker(attacker);

            var owner = (ICharacter)Owner;
            damage = PerformDefence(attacker, damageType, damage);

            damageSoaked = -1;
            double soakPercent = 0;
            switch (damageType)
            {
                case DamageType.FullMelee:
                case DamageType.StandardMelee:
                    soakPercent = GetBonus(BonusType.AbsorbMelee) * 0.01;
                    break;
                case DamageType.FullRange:
                case DamageType.StandardRange:
                    soakPercent = GetBonus(BonusType.AbsorbRange) * 0.01;
                    break;
                case DamageType.FullMagic:
                case DamageType.StandardMagic:
                    soakPercent = GetBonus(BonusType.AbsorbMagic) * 0.01;
                    break;
            }

            if (damage > 200)
            {
                var soak = (int)(damage * soakPercent);
                if (soak > 0)
                {
                    damageSoaked = soak;
                    damage -= soak;
                }
            }

            if (damage != -1)
            {
                damage = owner.Statistics.DamageLifePoints(damage);
                AddDamageToAttacker(attacker, damage);
            }

            if (owner.Profile.GetValue<bool>(ProfileConstants.CombatSettingsAutoRetaliate))
            {
                Owner.QueueTask(new RsTask(() => owner.Combat.SetTarget(attacker), 1));
            }

            return damage;
        }

        public override IRsTaskHandle<AttackResult> PerformAttack(AttackParams attackParams)
        {
            var handle = base.PerformAttack(attackParams);
            PerformSoulSplit(attackParams.Target, attackParams.Damage);
            handle.RegisterResultHandler(result =>
            {
                if (result.DamageLifePoints.Succeeded)
                {
                    AddExperience(attackParams.DamageType, result.DamageLifePoints.Count);
                }
            });
            return handle;
        }

        /// <summary>
        /// Ticks the attack
        /// </summary>
        protected override void AttackTick()
        {
            if (Target == null)
            {
                return;
            }

            DelayTick = 0;
            Target.Interrupt(this);

            LastAttacked = Target;

            var spell = GetCastedSpell();
            if (spell != null)
            {
                if (spell.CheckRequirements(_character))
                {
                    spell.RemoveRequirements(_character);
                    spell.PerformAttack(_character, Target);
                    OnAttackPerformed(Target);
                    if (_character.Magic.SelectedSpell != null) CancelTarget();
                }
                else
                {
                    _character.Magic.ClearAutoCastingSpell(false);
                    CancelTarget();
                }
            }
            else
            {
                if (_character.Appearance.IsTransformedToNpc())
                {
                    _character.Appearance.PnpcScript.PerformAttack(Target);
                }
                else
                {
                    var weapon = _character.Equipment[EquipmentSlot.Weapon];
                    if (weapon != null)
                    {
                        var specialAttack = _character.Profile.GetValue<bool>(ProfileConstants.CombatSettingsSpecialAttack);
                        if (specialAttack)
                        {
                            var requiredEnergyAmount = GetRequiredSpecialEnergyAmount();
                            if (_character.Statistics.SpecialEnergy < requiredEnergyAmount)
                            {
                                _character.SendChatMessage(GameStrings.NotEnoughSpecialEnergy);
                                _character.Mediator.Publish(new ProfileSetBoolAction(ProfileConstants.CombatSettingsSpecialAttack, false));
                                return;
                            }

                            _character.Statistics.DrainSpecialEnergy(requiredEnergyAmount);
                            _character.Mediator.Publish(new ProfileSetBoolAction(ProfileConstants.CombatSettingsSpecialAttack, false));
                            try
                            {
                                weapon.EquipmentScript.PerformSpecialAttack(weapon, _character, Target);
                                OnAttackPerformed(Target);
                                return;
                            }
                            catch (NotImplementedException) { }
                        }
                        else
                        {
                            try
                            {
                                weapon.EquipmentScript.PerformStandardAttack(weapon, _character, Target);
                                OnAttackPerformed(Target);
                                return;
                            }
                            catch (NotImplementedException) { }
                        }
                    }

                    // Unarmed - Standard anim.
                    Owner.QueueAnimation(_animationBuilder.Create().WithId(GetAttackStyle() == AttackStyle.MeleeAggressive ? 423 : 422).Build());
                    PerformAttack(new AttackParams()
                    {
                        Target = Target, Damage = GetMeleeDamage(Target, false), DamageType = DamageType.FullMelee, MaxDamage = GetMeleeMaxHit(Target, false)
                    });
                }

                OnAttackPerformed(Target);
            }
        }

        private void AddExperience(DamageType damageType, int damage)
        {
            switch (damageType)
            {
                case DamageType.FullMagic:
                case DamageType.StandardMagic:
                    AddMagicExperience(damage);
                    break;
                case DamageType.FullMelee:
                case DamageType.StandardMelee:
                    AddMeleeExperience(damage);
                    break;
                case DamageType.FullRange:
                case DamageType.StandardRange:
                    AddRangedExperience(damage);
                    break;
            }
        }


        /// <summary>
        /// Get's called after attack to specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnAttackPerformed(ICreature target)
        {
            if (_character.HasState(StateType.Turmoil)) _character.Statistics.SetTurmoilBonuses(target);

            foreach (var item in _character.Equipment)
            {
                item?.EquipmentScript.OnAttackPerformed(item, _character, target);
            }
        }

        /// <summary>
        /// Get's called after last attacked get's null.
        /// </summary>
        protected override void OnLastAttackedFade()
        {
            if (Owner.HasState(StateType.Turmoil)) _character.Statistics.ResetTurmoilBonuses();
        }

        /// <summary>
        /// Processes curses such as saps and leeches
        /// </summary>
        protected override void CursesTick()
        {
            var attackers = RecentAttackers;
            bool attack = false, defence = false, strength = false, ranged = false, magic = false;
            var self = new bool[5];
            foreach (var attacker in attackers)
            {
                var att = attacker.Attacker;
                if (att.HasState(StateType.SapWarrior) || att.HasState(StateType.LeechAttack)) attack = true;
                if (att.HasState(StateType.SapWarrior) || att.HasState(StateType.LeechStrength)) strength = true;
                if (att.HasState(StateType.SapWarrior) || att.HasState(StateType.SapRanger) || att.HasState(StateType.SapMager)
                    || att.HasState(StateType.LeechDefence))
                    defence = true;
                if (att.HasState(StateType.SapRanger) || att.HasState(StateType.LeechRanged)) ranged = true;
                if (att.HasState(StateType.SapMager) || att.HasState(StateType.LeechMagic)) magic = true;
            }

            {
                if (Owner.HasState(StateType.SapWarrior) || Owner.HasState(StateType.LeechAttack)) self[0] = true;
                if (Owner.HasState(StateType.SapWarrior) || Owner.HasState(StateType.LeechStrength)) self[1] = true;
                if (Owner.HasState(StateType.SapWarrior) || Owner.HasState(StateType.SapRanger) || Owner.HasState(StateType.SapMager)
                    || Owner.HasState(StateType.LeechDefence))
                    self[2] = true;
                if (Owner.HasState(StateType.SapRanger) || Owner.HasState(StateType.LeechRanged)) self[3] = true;
                if (Owner.HasState(StateType.SapMager) || Owner.HasState(StateType.LeechMagic)) self[4] = true;
            }

            if (!attack && GetPrayerBonus(BonusPrayerType.CurseInstantAttack) != 0)
                _character.Statistics.SetInstantCursePrayerBonus(BonusPrayerType.CurseInstantAttack, 0);
            if (!strength && GetPrayerBonus(BonusPrayerType.CurseInstantStrength) != 0)
                _character.Statistics.SetInstantCursePrayerBonus(BonusPrayerType.CurseInstantStrength, 0);
            if (!defence && GetPrayerBonus(BonusPrayerType.CurseInstantDefence) != 0)
                _character.Statistics.SetInstantCursePrayerBonus(BonusPrayerType.CurseInstantDefence, 0);
            if (!ranged && GetPrayerBonus(BonusPrayerType.CurseInstantRanged) != 0)
                _character.Statistics.SetInstantCursePrayerBonus(BonusPrayerType.CurseInstantRanged, 0);
            if (!magic && GetPrayerBonus(BonusPrayerType.CurseInstantMagic) != 0)
                _character.Statistics.SetInstantCursePrayerBonus(BonusPrayerType.CurseInstantMagic, 0);

            if (LastAttacked != null && DelayTick - 1 == 0) // -10 boost add time
            {
                for (var type = 13; type <= 17; type++)
                {
                    var bType = (BonusPrayerType)type;
                    if (self[type - 13] && LastAttacked.Combat.GetPrayerBonus(bType) != -10)
                    {
                        if (LastAttacked is ICharacter character)
                            character.Statistics.SetInstantCursePrayerBonus(bType, -10);
                        else if (LastAttacked is INpc npc) npc.Statistics.SetInstantCursePrayerBonus(bType, -10);
                    }
                }
            }

            var maxLuckTick = GetAttackSpeed();
            if (maxLuckTick <= 0) maxLuckTick = 1;

            if (LastAttacked == null)
            {
                return;
            }

            {
                var types = new int[7 + 4];
                var typesCount = 0;
                foreach (var state in Owner.GetStates())
                {
                    var type = (int)state.StateType;
                    if (type >= 14 && type <= 24) types[typesCount++] = type - 14;
                }

                if (typesCount > 0 && DelayTick - 1 == RandomStatic.Generator.Next(0, maxLuckTick) && RandomStatic.Generator.NextDouble() >= 0.80)
                {
                    var type = types[RandomStatic.Generator.Next(0, typesCount)];
                    if (type < 4) // sap
                    {
                        BonusPrayerType[]? drainTypes = null;
                        int shootGraphicID;
                        int projectileGraphicID;
                        int destinationGraphicID;

                        if (type == 0)
                        {
                            drainTypes =
                            [
                                BonusPrayerType.CurseAttack, BonusPrayerType.CurseStrength, BonusPrayerType.CurseDefence
                            ];
                            shootGraphicID = 2214;
                            projectileGraphicID = 2215;
                            destinationGraphicID = 2216;
                        }
                        else if (type == 1)
                        {
                            drainTypes =
                            [
                                BonusPrayerType.CurseRanged, BonusPrayerType.CurseDefence
                            ];
                            shootGraphicID = 2217;
                            projectileGraphicID = 2218;
                            destinationGraphicID = 2219;
                        }
                        else if (type == 2)
                        {
                            drainTypes =
                            [
                                BonusPrayerType.CurseMagic, BonusPrayerType.CurseDefence
                            ];
                            shootGraphicID = 2220;
                            projectileGraphicID = 2221;
                            destinationGraphicID = 2222;
                        }
                        else
                        {
                            shootGraphicID = 2223;
                            projectileGraphicID = 2224;
                            destinationGraphicID = 2225;
                        }

                        Owner.QueueAnimation(_animationBuilder.Create().WithId(12569).Build());
                        Owner.QueueGraphic(_graphicBuilder.Create().WithId(shootGraphicID).Build());
                        LastAttacked.QueueGraphic(_graphicBuilder.Create().WithId(destinationGraphicID).WithDelay(60).Build());
                        _projectileBuilder.Create()
                            .WithGraphicId(projectileGraphicID)
                            .FromCreature(Owner)
                            .ToCreature(LastAttacked)
                            .WithDuration(30)
                            .WithDelay(30)
                            .WithFromHeight(30)
                            .WithFromHeight(30)
                            .Send();

                        if (drainTypes != null)
                        {
                            var drainType = drainTypes[RandomStatic.Generator.Next(0, drainTypes.Length)];
                            var drained = false;
                            if (LastAttacked is ICharacter character)
                                drained = character.Statistics.DecreaseCursePrayerBonus(drainType, -10);
                            else if (LastAttacked is INpc npc) drained = npc.Statistics.DecreaseCursePrayerBonus(drainType, -10);

                            if (drained)
                                _character.SendChatMessage("Your curse drains the enemy's " + CreatureHelperTwo.GetCurseSkillName(drainType) + ".");
                            else
                                _character.SendChatMessage("Your opponent has been weakened so much that your sap curse has no effect.");
                        }
                        else if (LastAttacked is ICharacter character)
                        {
                            _character.SendChatMessage("Your sap curse drains your enemy.");
                            if (character.Statistics.SpecialEnergy < 100 || character.Statistics.DrainSpecialEnergy(100) < 100)
                                _character.SendChatMessage("Your opponent has too little special attack energy for your curse to take effect.");
                        }
                        else
                            _character.SendChatMessage("Your sap curse doesn't affect your enemy. It should be used against other players instead.");
                    }
                    else
                    {
                        var drainType = -1;
                        int projectileGraphicID;
                        int destinationGraphicID;

                        switch (type)
                        {
                            case 4:
                                drainType = (int)BonusPrayerType.CurseAttack;
                                projectileGraphicID = 2231;
                                destinationGraphicID = 2232;
                                break;
                            case 5:
                                drainType = (int)BonusPrayerType.CurseStrength;
                                projectileGraphicID = 2248;
                                destinationGraphicID = 2250;
                                break;
                            case 6:
                                drainType = (int)BonusPrayerType.CurseDefence;
                                projectileGraphicID = 2244;
                                destinationGraphicID = 2246;
                                break;
                            case 7:
                                drainType = (int)BonusPrayerType.CurseRanged;
                                projectileGraphicID = 2236;
                                destinationGraphicID = 2238;
                                break;
                            case 8:
                                drainType = (int)BonusPrayerType.CurseMagic;
                                projectileGraphicID = 2240;
                                destinationGraphicID = 2242;
                                break;
                            case 9:
                                drainType = -1;
                                projectileGraphicID = 2252;
                                destinationGraphicID = 2254;
                                break;
                            default:
                                drainType = -1;
                                projectileGraphicID = 2256;
                                destinationGraphicID = 2258;
                                break;
                        }

                        Owner.QueueAnimation(_animationBuilder.Create().WithId(12575).Build());
                        LastAttacked.QueueGraphic(_graphicBuilder.Create().WithId(destinationGraphicID).WithDelay(60).Build());
                        _projectileBuilder.Create()
                            .WithGraphicId(projectileGraphicID)
                            .FromCreature(Owner)
                            .ToCreature(LastAttacked)
                            .WithDuration(30)
                            .WithDelay(30)
                            .WithFromHeight(30)
                            .WithToHeight(30)
                            .Send();

                        if (drainType != -1)
                        {
                            var drained = false;
                            if (LastAttacked is ICharacter character)
                                drained = character.Statistics.DecreaseCursePrayerBonus((BonusPrayerType)drainType, -15);
                            else if (LastAttacked is INpc npc) drained = npc.Statistics.DecreaseCursePrayerBonus((BonusPrayerType)drainType, -15);

                            if (drained)
                            {
                                if (_character.Statistics.IncreaseCursePrayerBonus((BonusPrayerType)drainType, 10))
                                    _character.SendChatMessage("Your curse drains " + CreatureHelperTwo.GetCurseSkillName((BonusPrayerType)drainType) +
                                                               " from the enemy, boosting your " +
                                                               CreatureHelperTwo.GetCurseSkillName((BonusPrayerType)drainType) + ".");
                                else
                                    _character.SendChatMessage("Your curse drains " + CreatureHelperTwo.GetCurseSkillName((BonusPrayerType)drainType) +
                                                               " from the enemy, but has already made you so strong that it can improve you no further.");
                            }
                            else
                                _character.SendChatMessage("Your opponent has been weakened so much that your leech curse has no effect.");
                        }
                        else if (type == 9)
                        {
                            if (LastAttacked is ICharacter character)
                            {
                                _character.SendChatMessage("Your leech curse drains your enemy.");
                                if (character.Statistics.RunEnergy >= 10 && character.Statistics.DrainRunEnergy(10) >= 10)
                                {
                                    if (_character.Statistics.HealRunEnergy(10) > 0) _character.SendChatMessage("You leech some run energy from your enemy.");
                                }
                                else
                                    _character.SendChatMessage("Your opponent has too little run energy for your curse to take effect.");
                            }
                            else
                                _character.SendChatMessage("Your leech curse doesn't affect your enemy. It should be used against other players instead.");
                        }
                        else if (type == 10)
                        {
                            if (LastAttacked is ICharacter character)
                            {
                                _character.SendChatMessage("Your leech curse drains your enemy.");
                                if (character.Statistics.SpecialEnergy >= 100 && character.Statistics.DrainSpecialEnergy(100) >= 100)
                                {
                                    if (_character.Statistics.HealSpecialEnergy(100) > 0)
                                        _character.SendChatMessage("You leech some special energy from your enemy.");
                                }
                                else
                                    _character.SendChatMessage("Your opponent has too little special energy for your curse to take effect.");
                            }
                            else
                                _character.SendChatMessage("Your leech curse doesn't affect your enemy. It should be used against other players instead.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate's ranged damage to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <param name="specialAttack">Wheter character is using special attack.</param>
        /// <returns>Calculated damage.</returns>
        public int GetRangedDamage(ICreature target, bool specialAttack)
        {
            double myAttackLevel = GetRangedLevel();
            //int myDefenceLevel = this.GetDefenceLevel();
            var enemyDefenceLevel = target.Combat.GetDefenceLevel();

            if (GetAttackStyle() == AttackStyle.RangedAccurate) myAttackLevel += 3;
            if (target.Combat.GetAttackStyle() == AttackStyle.MeleeDefensive)
                enemyDefenceLevel += 3;
            else if (target.Combat.GetAttackStyle() == AttackStyle.MeleeControlled) enemyDefenceLevel += 1;

            if (Owner.HasState(StateType.VoidRangedEquiped)) myAttackLevel *= 1.1;

            var effectiveAttack = myAttackLevel;
            effectiveAttack += 8.0;
            effectiveAttack += GetAttackBonus();
            effectiveAttack = Math.Round(effectiveAttack);


            double effectiveDefence = enemyDefenceLevel;
            effectiveDefence += 8.0;
            effectiveDefence += target.Combat.GetDefenceBonus(GetAttackBonusType());
            effectiveDefence = Math.Round(effectiveDefence);


            double attackersRoll = (int)Math.Round(effectiveAttack * (1.0 + GetAttackBonus() / 16.0) * 10.0); // / 64
            double defendersRoll = (int)Math.Round(effectiveDefence * (1.0 + target.Combat.GetDefenceBonus(GetAttackBonusType()) / 64.0) * 10.0);

            if (specialAttack)
            {
                if (Owner.HasState(StateType.MagicShortBowEquiped)) attackersRoll *= 1.1;
            }

            var accuracy = 0.5;
            if (attackersRoll < defendersRoll)
                accuracy = (attackersRoll - 1.0) / (2.0 * defendersRoll);
            else if (attackersRoll > defendersRoll) accuracy = 1.0 - (defendersRoll + 1.0) / (2.0 * attackersRoll);

            if (specialAttack)
            {
                if (Owner.HasState(StateType.CrossbowEquiped))
                {
                    if (Owner.HasState(StateType.EnchantedDragonstoneBoltsEquiped)) accuracy = 3.0; // very high power to damage calculation
                }
            }

            if (RandomStatic.Generator.NextDouble() > accuracy) return -1;

            var max = GetRangedMaxHit(target, specialAttack);
            return CreatureHelperTwo.PerformRangedDamageCalculation(1, max, accuracy);
        }

        /// <summary>
        /// Get's ranged max hit to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <param name="usingSpecial">Wheter character is using special attack.</param>
        /// <returns>System.Int32.</returns>
        public int GetRangedMaxHit(ICreature target, bool usingSpecial)
        {
            var owner = (ICharacter)Owner;
            var myRangedLevel = GetRangedLevel();
            var myRangedStrengthBonus = GetBonus(BonusType.RangedStrength);
            var style = GetAttackStyle();
            switch (style)
            {
                case AttackStyle.MeleeAggressive: myRangedLevel += 3; break;
                case AttackStyle.MeleeControlled: myRangedLevel += 1; break;
            }

            var baseDamage = 5.0 + (myRangedLevel + 8.0) * (myRangedStrengthBonus + 64.0) / 64.0;
            var specialBonus = 1.0;

            if (usingSpecial)
            {
                if (owner.HasState(StateType.CrossbowEquiped))
                {
                    if (owner.HasState(StateType.EnchantedOpalBoltsEquipped)) specialBonus = 1.25;
                    if (owner.HasState(StateType.EnchantedDiamondBoltsEquipped)) specialBonus = 1.15;
                    if (owner.HasState(StateType.EnchantedDragonstoneBoltsEquiped)) specialBonus = 1.45;
                    if (owner.HasState(StateType.EnchantedOnyxBoltsEquiped)) specialBonus = 1.15;
                }

                if (owner.HasState(StateType.MorrigansThrownAxeEquiped)) specialBonus = 1.2;
                if (owner.HasState(StateType.DarkBowEquiped)) specialBonus = owner.HasState(StateType.DragonArrowsEquiped) ? 1.5 : 1.3;
            }

            if (owner.HasState(StateType.VoidRangedEquiped)) baseDamage *= 1.1;

            if (usingSpecial && owner.HasState(StateType.ZanikCrossbowEquiped))
                return (int)Math.Floor(baseDamage) + 150;
            else if (usingSpecial)
                return (int)Math.Floor(baseDamage * specialBonus);
            else
                return (int)Math.Truncate(baseDamage);
        }


        /// <summary>
        /// Calculate's melee damage to specific target.
        /// Adds experience to owner skills according to calculated damage.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <param name="specialAttack">Wheter character is using special attack.</param>
        /// <returns>Calculated damage.</returns>
        public int GetMeleeDamage(ICreature target, bool specialAttack)
        {
            var owner = (ICharacter)Owner;

            double myAttackLevel = GetAttackLevel();
            var enemyDefenceLevel = target.Combat.GetDefenceLevel();

            if (GetAttackStyle() == AttackStyle.MeleeAccurate)
                myAttackLevel += 3;
            else if (GetAttackStyle() == AttackStyle.MeleeControlled) myAttackLevel++;
            if (target.Combat.GetAttackStyle() == AttackStyle.MeleeDefensive)
                enemyDefenceLevel += 3;
            else if (target.Combat.GetAttackStyle() == AttackStyle.MeleeControlled) enemyDefenceLevel += 1;

            if (owner.HasState(StateType.VoidMeleeEquiped)) myAttackLevel *= 1.1;

            var effectiveAttack = myAttackLevel;
            effectiveAttack += 8.0;
            effectiveAttack += GetAttackBonus();
            effectiveAttack = Math.Round(effectiveAttack);


            double effectiveDefence = enemyDefenceLevel;
            effectiveDefence += 8.0;
            effectiveDefence += target.Combat.GetDefenceBonus(GetAttackBonusType());
            effectiveDefence = Math.Round(effectiveDefence);


            double attackersRoll = (int)Math.Round(effectiveAttack * (1.0 + GetAttackBonus() / 16.0) * 10.0); // / 64
            double defendersRoll = (int)Math.Round(effectiveDefence * (1.0 + target.Combat.GetDefenceBonus(GetAttackBonusType()) / 64.0) * 10.0);

            if (specialAttack)
            {
                if (owner.HasState(StateType.DragonDaggerEquipped)) attackersRoll *= 1.1;
                if (owner.HasState(StateType.AbyssalWhipEquipped)) attackersRoll *= 1.1;
            }

            var accuracy = 0.5;
            if (attackersRoll < defendersRoll)
                accuracy = (attackersRoll - 1.0) / (2.0 * defendersRoll);
            else if (attackersRoll > defendersRoll) accuracy = 1.0 - (defendersRoll + 1.0) / (2.0 * attackersRoll);

            if (specialAttack)
            {
                if (owner.HasState(StateType.KorasiEquipped)) accuracy = 1.0; // 100%
            }

            if (RandomStatic.Generator.NextDouble() > accuracy) return -1;

            var max = GetMeleeMaxHit(target, specialAttack);
            return CreatureHelperTwo.PerformMeleeDamageCalculation(1, max, accuracy);
        }

        /// <summary>
        /// Get's melee max hit to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <param name="usingSpecial">Wheter character is using special attack.</param>
        /// <returns>System.Int32.</returns>
        public int GetMeleeMaxHit(ICreature target, bool usingSpecial)
        {
            var owner = (ICharacter)Owner;
            var myStrengthLevel = GetStrengthLevel();
            var myStrengthBonus = GetBonus(BonusType.Strength);
            var style = GetAttackStyle();
            switch (style)
            {
                case AttackStyle.MeleeAggressive: myStrengthLevel += 3; break;
                case AttackStyle.MeleeControlled: myStrengthLevel += 1; break;
            }

            var baseDamage = 13.0 + myStrengthLevel + myStrengthBonus / 8.0 + myStrengthLevel * (double)myStrengthBonus / 64.0;
            var maxHitModifier = 1.0;

            if (usingSpecial)
            {
                if (owner.HasState(StateType.DragonDaggerEquipped)) maxHitModifier = 1.15;
                if (owner.HasState(StateType.ArmadylGodswordEquipped)) maxHitModifier = 1.25;
                if (owner.HasState(StateType.BandosGodswordEquipped)) maxHitModifier = 1.10;
                if (owner.HasState(StateType.SaradominGodswordEquipped)) maxHitModifier = 1.10;
                if (owner.HasState(StateType.VestaLongswordEquipped)) maxHitModifier = 1.2;
                if (owner.HasState(StateType.StatiusWarhammerEquipped)) maxHitModifier = 1.25;
                if (owner.HasState(StateType.KorasiEquipped)) maxHitModifier = 1.5;
            }

            if (owner.HasState(StateType.VoidMeleeEquiped))
                baseDamage *= 1.1;
            else if (owner.HasState(StateType.DharokWretchedStrength))
            {
                if (RandomStatic.Generator.NextDouble() >= 0.50)
                {
                    double hp = owner.Statistics.LifePoints;
                    double maxhp = owner.Statistics.GetMaximumLifePoints();
                    var d = hp / maxhp;
                    maxHitModifier = 2.0 - d;
                }
            }

            if (usingSpecial || maxHitModifier > 1.0)
                return (int)Math.Floor(baseDamage * maxHitModifier);
            else
                return (int)Math.Truncate(baseDamage);
        }


        /// <summary>
        /// Get's magic max hit to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <param name="baseDamage">Contains base magic damage the spell does.</param>
        /// <returns>System.Int32.</returns>
        public int GetMagicMaxHit(ICreature target, int baseDamage)
        {
            var owner = (ICharacter)Owner;
            var myMagicLevel = owner.Statistics.GetSkillLevel(StatisticsConstants.Magic);
            var myMaxMagicLevel = owner.Statistics.LevelForExperience(StatisticsConstants.Magic);
            var myMagicDamageBonus = GetBonus(BonusType.MagicDamage);
            double damage = baseDamage;
            for (var i = myMaxMagicLevel + 1; i <= myMagicLevel; i++) damage *= 1.03;
            damage *= 1.0 + myMagicDamageBonus / 100.0;
            return (int)Math.Truncate(damage);
        }


        /// <summary>
        /// Calculate's magic damage to specific target.
        /// </summary>
        /// <param name="target">Target which is being hit.</param>
        /// <param name="baseDamage">Base spell maximum damage without bonuses.</param>
        /// <returns>Calculated damage.</returns>
        public int GetMagicDamage(ICreature target, int baseDamage)
        {
            var owner = (ICharacter)Owner;

            double myAttackLevel = GetMagicLevel();
            var enemyMagicDefenceLevel = target.Combat.GetMagicLevel();
            var enemyDefenceLevel = target.Combat.GetDefenceLevel();

            if (target.Combat.GetAttackStyle() == AttackStyle.MeleeDefensive)
                enemyDefenceLevel += 3;
            else if (target.Combat.GetAttackStyle() == AttackStyle.MeleeControlled) enemyDefenceLevel += 3;

            if (owner.HasState(StateType.VoidMagicEquiped)) myAttackLevel *= 1.3;

            var effectiveAttack = myAttackLevel;
            effectiveAttack += 8.0;
            effectiveAttack += GetAttackBonus();
            effectiveAttack = Math.Round(effectiveAttack);

            double effectiveDefence = enemyDefenceLevel;
            effectiveDefence += 8.0;
            effectiveDefence += target.Combat.GetDefenceBonus(GetAttackBonusType());
            effectiveDefence = Math.Round(effectiveDefence);

            var effectiveMagicDefence = Math.Round((double)enemyMagicDefenceLevel);
            effectiveMagicDefence *= 0.7;
            effectiveDefence *= 0.3;

            effectiveDefence = Math.Round(effectiveDefence);
            effectiveMagicDefence = Math.Round(effectiveMagicDefence);

            effectiveDefence += effectiveMagicDefence;


            double attackersRoll = (int)Math.Round(effectiveAttack * (1.0 + GetAttackBonus() / 16.0) * 10.0); // / 64
            double defendersRoll = (int)Math.Round(effectiveDefence * (1.0 + target.Combat.GetDefenceBonus(GetAttackBonusType()) / 64.0) * 10.0);

            var accuracy = 0.5;
            if (attackersRoll < defendersRoll)
                accuracy = (attackersRoll - 1.0) / (2.0 * defendersRoll);
            else if (attackersRoll > defendersRoll) accuracy = 1.0 - (defendersRoll + 1.0) / (2.0 * attackersRoll);

            if (RandomStatic.Generator.NextDouble() > accuracy) return -1;

            var max = GetMagicMaxHit(target, baseDamage);
            return CreatureHelperTwo.PerformMagicDamageCalculation(1, max, accuracy);
        }


        /// <summary>
        /// Add's melee experience for specific hit if it's &gt; 0.
        /// </summary>
        /// <param name="hit">The hit.</param>
        public void AddMeleeExperience(int hit)
        {
            if (hit <= 0) return;
            var owner = (ICharacter)Owner;
            owner.Statistics.AddExperience(StatisticsConstants.Constitution, hit * 0.133);
            switch (GetAttackStyle())
            {
                case AttackStyle.MeleeAccurate:
                    {
                        owner.Statistics.AddExperience(StatisticsConstants.Attack, hit * 0.4);
                        break;
                    }
                case AttackStyle.MeleeAggressive:
                    {
                        owner.Statistics.AddExperience(StatisticsConstants.Strength, hit * 0.4);
                        break;
                    }
                case AttackStyle.MeleeControlled:
                    {
                        owner.Statistics.AddExperience(StatisticsConstants.Attack, hit * 0.133);
                        owner.Statistics.AddExperience(StatisticsConstants.Defence, hit * 0.133);
                        owner.Statistics.AddExperience(StatisticsConstants.Strength, hit * 0.133);
                        break;
                    }
                case AttackStyle.MeleeDefensive:
                    {
                        owner.Statistics.AddExperience(StatisticsConstants.Defence, hit * 0.4);
                        break;
                    }
            }
        }

        /// <summary>
        /// Adds ranged experience for specific hit if it's &gt; 0.
        /// </summary>
        /// <param name="hit">The hit.</param>
        public void AddRangedExperience(int hit)
        {
            if (hit <= 0) return;
            var owner = (ICharacter)Owner;
            owner.Statistics.AddExperience(StatisticsConstants.Constitution, hit * 0.133);
            switch (GetAttackStyle())
            {
                case AttackStyle.RangedAccurate:
                case AttackStyle.RangedRapid:
                    {
                        owner.Statistics.AddExperience(StatisticsConstants.Ranged, hit * 0.4);
                        break;
                    }
                case AttackStyle.RangedLongRange:
                    {
                        owner.Statistics.AddExperience(StatisticsConstants.Ranged, hit * 0.2);
                        owner.Statistics.AddExperience(StatisticsConstants.Defence, hit * 0.2);
                        break;
                    }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Add's magic experience for specific hit if it's &gt; 0.
        /// </summary>
        /// <param name="damage">The hit.</param>
        public void AddMagicExperience(int damage)
        {
            if (damage <= 0) return;
            var owner = (ICharacter)Owner;
            owner.Statistics.AddExperience(StatisticsConstants.Constitution, damage * 0.133);
            switch (GetAttackStyle())
            {
                case AttackStyle.MagicNormal:
                    {
                        owner.Statistics.AddExperience(StatisticsConstants.Magic, damage * 0.2);
                        break;
                    }
                case AttackStyle.MagicDefensive:
                    {
                        owner.Statistics.AddExperience(StatisticsConstants.Defence, damage * 0.133);
                        owner.Statistics.AddExperience(StatisticsConstants.Magic, damage * 0.133);
                        break;
                    }
            }
        }


        /// <summary>
        /// Renders defence.
        /// </summary>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage.</param>
        /// <returns>Amount of damage remains after defence.</returns>
        public int PerformDefence(ICreature attacker, DamageType damageType, int damage)
        {
            foreach (var item in _character.Equipment)
            {
                item?.EquipmentScript.OnAttack(item, _character, attacker, damageType, ref damage);
            }

            if (damage <= 0) damage = -1;

            if (damage == -1 || !_character.HasState(StateType.Vengeance) || attacker.HasState(StateType.VengeanceImmunity) ||
                damageType == DamageType.Reflected || damageType == DamageType.Standard || damageType == DamageType.DragonFire)
            {
                return damage;
            }

            var soaked = -1;
            var reflectBack = attacker.Combat.Attack(_character, DamageType.Reflected, (int)(damage * 0.75), ref soaked);
            if (reflectBack < 1)
            {
                return damage;
            }

            _character.Speak("Taste Vengeance!");
            _character.RemoveState(StateType.Vengeance);
            var reflectSplat = _character.ServiceProvider.GetRequiredService<IHitSplatBuilder>()
                .Create()
                .AddSprite(builder => builder.WithDamage(reflectBack).WithSplatType(HitSplatType.HitSimpleDamage))
                .AddSprite(builder => builder.WithDamage(soaked).WithSplatType(HitSplatType.HitDefendedDamage))
                .Build();
            attacker.QueueHitSplat(reflectSplat);
            return damage;
        }


        /// <summary>
        /// Get's attack distance of this character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int GetAttackDistance()
        {
            if (_character.Appearance.IsTransformedToNpc()) return _character.Appearance.PnpcScript.GetAttackDistance();
            var spell = GetCastedSpell();
            if (spell != null) return spell.GetCombatDistance(_character);
            var weapon = _character.Equipment[EquipmentSlot.Weapon];
            if (weapon == null) return 1;
            var dist = weapon.EquipmentScript.GetAttackDistance(weapon);
            if (GetAttackStyle() == AttackStyle.RangedLongRange) dist += 3;
            return dist;
        }

        /// <summary>
        /// Gets attack speed of this character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int GetAttackSpeed()
        {
            if (_character.Appearance.IsTransformedToNpc()) return _character.Appearance.PnpcScript.GetAttackSpeed();
            var spell = GetCastedSpell();
            int speed;
            var weapon = _character.Equipment[EquipmentSlot.Weapon];
            if (spell != null)
            {
                speed = spell.GetCastingSpeed(_character);
            }
            else if (weapon == null)
            {
                speed = 4; // as fast as scimitar 2.4 sec
            }
            else
                speed = weapon.EquipmentScript.GetAttackSpeed(weapon);

            if (GetAttackStyle() == AttackStyle.RangedRapid) speed -= 1;
            if (spell == null && Owner.HasState(StateType.MiasmicSlow)) speed *= 2;
            return speed;
        }

        /// <summary>
        /// Get's attack bonus type of this character.
        /// </summary>
        /// <returns>AttackBonus.</returns>
        public override AttackBonus GetAttackBonusType()
        {
            if (_character.Appearance.IsTransformedToNpc()) return _character.Appearance.PnpcScript.GetAttackBonusType();
            var spell = GetCastedSpell();
            if (spell != null) return AttackBonus.Magic;
            if (_character.Equipment[EquipmentSlot.Weapon] == null) return AttackBonus.Crush;
            return _character.Equipment[EquipmentSlot.Weapon]!.EquipmentScript.GetAttackBonusType(_character.Equipment[EquipmentSlot.Weapon]!,
                _character.Profile.GetValue<int>(ProfileConstants.CombatSettingsAttackStyleOptionId));
        }

        /// <summary>
        /// Get's attack style of this character.
        /// </summary>
        /// <returns>AttackStyle.</returns>
        public override AttackStyle GetAttackStyle()
        {
            if (_character.Appearance.IsTransformedToNpc()) return _character.Appearance.PnpcScript.GetAttackStyle();
            if (GetCastedSpell() != null)
                return _character.Profile.GetValue<bool>(ProfileConstants.CombatSettingsMagicDefensiveCasting)
                    ? AttackStyle.MagicDefensive
                    : AttackStyle.MagicNormal;
            if (_character.Equipment[EquipmentSlot.Weapon] != null)
            {
                return _character.Equipment[EquipmentSlot.Weapon]!.EquipmentScript.GetAttackStyle(_character.Equipment[EquipmentSlot.Weapon]!,
                    _character.Profile.GetValue<int>(ProfileConstants.CombatSettingsAttackStyleOptionId));
            }

            return _character.Profile.GetValue<int>(ProfileConstants.CombatSettingsAttackStyleOptionId) switch
            {
                0 => AttackStyle.MeleeAccurate,
                1 => AttackStyle.MeleeAggressive,
                2 => AttackStyle.MeleeDefensive,
                _ => AttackStyle.MeleeAccurate,
            };
        }

        /// <summary>
        /// Get's casted spell if character is casting it.
        /// </summary>
        /// <returns>CombatSpell.</returns>
        public ICombatSpell? GetCastedSpell()
        {
            ICombatSpell? spell = null;
            if (_character.Magic.SelectedSpell != null) spell = _character.Magic.SelectedSpell;
            if (spell == null && _character.Magic.AutoCastingSpell != null) spell = _character.Magic.AutoCastingSpell;
            return spell;
        }

        /// <summary>
        /// Get's specific bonus.
        /// </summary>
        /// <param name="bonusType">Type of the bonus.</param>
        /// <returns>System.Int32.</returns>
        public override int GetBonus(BonusType bonusType) => _character.Statistics.Bonuses.GetBonus(bonusType);

        /// <summary>
        /// Get's specific prayer bonus.
        /// </summary>
        /// <param name="bonusType">Type of the bonus.</param>
        /// <returns>System.Int32.</returns>
        public override int GetPrayerBonus(BonusPrayerType bonusType) => _character.Statistics.PrayerBonuses.GetBonus(bonusType);

        /// <summary>
        /// Get's attack level of this character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int GetAttackLevel()
        {
            double baseLevel = ((ICharacter)Owner).Statistics.GetSkillLevel(StatisticsConstants.Attack);
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.StaticAttack) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseAttack) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseInstantAttack) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.TurmoilAttack) * 0.01;

            baseLevel = (int)baseLevel; // truncate
            // here add other bonuses such as void & etc
            // -----------------------------------------
            return (int)baseLevel;
        }

        /// <summary>
        /// Get's strength level of this character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int GetStrengthLevel()
        {
            double baseLevel = ((ICharacter)Owner).Statistics.GetSkillLevel(StatisticsConstants.Strength);
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.StaticStrength) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseStrength) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseInstantStrength) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.TurmoilStrength) * 0.01;
            baseLevel = (int)baseLevel; // truncate
            // here add other bonuses such as dharok & etc
            // -------------------------------------------
            return (int)baseLevel;
        }

        /// <summary>
        /// Get's defence level of this character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int GetDefenceLevel()
        {
            double baseLevel = ((ICharacter)Owner).Statistics.GetSkillLevel(StatisticsConstants.Defence);
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.StaticDefence) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseDefence) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseInstantDefence) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.TurmoilDefence) * 0.01;
            baseLevel = (int)baseLevel; // truncate
            // here add other bonuses such as void & etc
            // -------------------------------------------
            return (int)baseLevel;
        }

        /// <summary>
        /// Get's ranged level of this character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int GetRangedLevel()
        {
            double baseLevel = ((ICharacter)Owner).Statistics.GetSkillLevel(StatisticsConstants.Ranged);
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.StaticRanged) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseRanged) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseInstantRanged) * 0.01;
            baseLevel = (int)baseLevel; // truncate
            // here add other bonuses such as void & etc
            // -------------------------------------------
            return (int)baseLevel;
        }

        /// <summary>
        /// Get's magic level of this character.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int GetMagicLevel()
        {
            double baseLevel = ((ICharacter)Owner).Statistics.GetSkillLevel(StatisticsConstants.Magic);
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.StaticMagic) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseMagic) * 0.01;
            baseLevel *= 1.0 + GetPrayerBonus(BonusPrayerType.CurseInstantMagic) * 0.01;
            baseLevel = (int)baseLevel; // truncate
            // here add other bonuses such as void & etc
            // -------------------------------------------
            return (int)baseLevel;
        }

        /// <summary>
        /// Get's required special energy amount for special attack.
        /// Returns 0 if weapon doesn't have special attack or special doesn't need any energy.
        /// </summary>
        /// <returns>System.Int16.</returns>
        public int GetRequiredSpecialEnergyAmount()
        {
            var owner = (ICharacter)Owner;
            var weapon = owner.Equipment[EquipmentSlot.Weapon];
            return weapon == null ? 0 : weapon.EquipmentScript.GetRequiredSpecialEnergyAmount(weapon, owner);
        }
    }
}