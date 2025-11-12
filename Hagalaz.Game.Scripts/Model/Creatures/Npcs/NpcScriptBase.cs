using System.Linq;
using System;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    /// Base NPCScript class.
    /// </summary>
    public abstract class NpcScriptBase : INpcScript
    {
        /// <summary>
        /// Contains owner npc.
        /// </summary>
        /// <value>The owner.</value>
        protected INpc Owner;

        /// <summary>
        /// <summary>
        /// Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>System.Int32[][].</returns>
        [Obsolete("Use an NpcScriptFactory or NpcScriptMetaData instead")]
        public virtual int[] GetSuitableNpcs() => [];

        /// <summary>
        /// Get's called when owner is found.
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Initializes this script with given owner.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public void Initialize(INpc owner)
        {
            Owner = owner;
            Initialize();
        }

        /// <summary>
        /// Respawns this npc.
        /// By default, this unregisters the NPC if the CanSpawn method returns false.
        /// Otherwise, this calls the Respawn method of the NPC.
        /// </summary>
        public virtual void Respawn()
        {
            if (CanSpawn())
                Owner.Respawn();
            else
                Owner.ServiceProvider.GetRequiredService<INpcService>().UnregisterAsync(Owner).Wait();
        }

        /// <summary>
        /// Get's called when npc is spawned.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnSpawn() { }

        /// <summary>
        /// Get's called when npc is dead.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnDeath() { }

        /// <summary>
        /// Get's called when npc is destroyed permanently.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnDestroy() { }

        /// <summary>
        /// Get's called when npc is created.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void OnCreate() { }

        /// <summary>
        /// Get's called when npc killed its target.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void OnTargetKilled(ICreature target) { }

        /// <summary>
        /// Get's called when npc is killed.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="killer">The killer.</param>
        public virtual void OnKilledBy(ICreature killer) { }

        /// <summary>
        /// Called when [set target].
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void OnSetTarget(ICreature target) { }

        /// <summary>
        /// Called when [cancel target].
        /// By default, this method will let the NPC walk to its spawnpoint.
        /// </summary>
        public virtual void OnCancelTarget() => Owner.QueueTask(new NpcSpawnPointReachTask(Owner, true));

        /// <summary>
        /// Uses the item on NPC.
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public virtual bool UseItemOnNpc(IItem used, ICharacter character) => false;

        /// <summary>
        /// Get's called when npc is interrupted.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="source">Object which performed the interruption,
        /// this parameter can be null , but it is not encouraged to do so.
        /// Best use would be to set the invoker class instance as source.</param>
        public virtual void OnInterrupt(object source) { }

        /// <summary>
        /// Determines whether this instance [can set target] the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public virtual bool CanSetTarget(ICreature target)
        {
            if (!Owner.Viewport.VisibleCreatures.Contains(target)) return false;
            if (!target.Area.Script.CanBeAttacked(target, Owner)) return false;
            if (!Owner.Area.Script.CanAttack(Owner, target)) return false;
            if (!CanAttack(target)) return false;
            if (!target.Combat.CanBeAttackedBy(Owner)) return false;
            return true;
        }

        /// <summary>
        /// Get's if this npc can attack the specified character ('target').
        /// By default , this method returns true.
        /// </summary>
        /// <param name="target">Creature which is being attacked by this npc.</param>
        /// <returns>If attack can be performed.</returns>
        public virtual bool CanAttack(ICreature target)
        {
            if (!Owner.Viewport.VisibleCreatures.Contains(target)) return false;
            return Owner.Bounds.DefaultLocation.WithinDistance(target.Location, CreatureConstants.VisibilityDistance);
        }

        /// <summary>
        /// Get's if this npc can be attacked by the specified character ('attacker').
        /// By default , this method does check if this npc is attackable.
        /// This method also checks if the attacker is a character, wether or not it
        /// has the required slayer level.
        /// </summary>
        /// <param name="attacker">Creature which is attacking this npc.</param>
        /// <returns>If attack can be performed.</returns>
        public virtual bool CanBeAttackedBy(ICreature attacker)
        {
            if (!Owner.Viewport.VisibleCreatures.Contains(attacker)) return false;

            var att = attacker as ICharacter;
            if (att?.Statistics.GetSkillLevel(StatisticsConstants.Slayer) < Owner.Definition.SlayerLevelRequired)
            {
                att.SendChatMessage(
                    "You need to have a slayer level of at least " + Owner.Definition.SlayerLevelRequired + " in order to attack this creature.");
                return false;
            }

            return Owner.Definition.Attackable && Owner.Bounds.DefaultLocation.WithinDistance(attacker.Location, CreatureConstants.VisibilityDistance);
        }

        /// <summary>
        /// Determines whether this instance [can be looted by] the specified killer.
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns></returns>
        public virtual bool CanBeLootedBy(ICreature killer)
        {
            if (Owner.Definition.LootTableId <= 0) return false;
            return killer is ICharacter;
        }

        /// <summary>
        /// Render's npc death
        /// </summary>
        /// <returns>Amount of ticks the death gonna be rendered.</returns>
        public virtual int RenderDeath()
        {
            if (Owner.Definition.DeathAnimation != -1) Owner.QueueAnimation(Animation.Create(Owner.Definition.DeathAnimation));
            if (Owner.Definition.DeathGraphic != -1) Owner.QueueGraphic(Graphic.Create(Owner.Definition.DeathGraphic));
            return Owner.Definition.DeathTicks;
        }

        /// <summary>
        /// Get's attack bonus type of this npc.
        /// By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>AttackBonus.</returns>
        public virtual AttackBonus GetAttackBonusType() => AttackBonus.Crush;

        /// <summary>
        /// Get's attack style of this npc.
        /// By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>AttackStyle.</returns>
        public virtual AttackStyle GetAttackStyle() => AttackStyle.MeleeAccurate;

        /// <summary>
        /// Get's attack distance of this npc.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public virtual int GetAttackDistance() => 1;

        /// <summary>
        /// Get's attack speed of this npc.
        /// By default, this method does return Definition.AttackSpeed.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public virtual int GetAttackSpeed() => Owner.Definition.AttackSpeed;

        /// <summary>
        /// Get's if this npc can retaliate to specific character attack.
        /// By default, this method returns true.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns><c>true</c> if this instance [can retaliate to] the specified creature; otherwise, <c>false</c>.</returns>
        public virtual bool CanRetaliateTo(ICreature creature) => true;

        /// <summary>
        /// Contains the path finder the NPC will use.
        /// By default, this method returns the simple path finder when in combat
        /// and the standart path finder for random walking.
        /// </summary>
        public virtual IPathFinder GetPathfinder() => Owner.ServiceProvider.GetRequiredService<IPathFinderProvider>().Simple;

        /// <summary>
        /// Get's if this npc can aggro attack specific character.
        /// By default this method does check if creature is character.
        /// This method does not get called by ticks if npc reaction is not aggresive.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns><c>true</c> if this instance can aggro the specified creature; otherwise, <c>false</c>.</returns>
        public virtual bool IsAggressiveTowards(ICreature creature)
        {
            if (creature is not ICharacter character)
            {
                return false;
            }

            if (!Owner.WithinRange(creature, GetAttackDistance() + 2)) return false;
            if (!creature.Area.MultiCombat)
            {
                if (Owner.Viewport.VisibleCreatures.OfType<INpc>().Any(npc => npc.Combat.LastAttacked == creature))
                {
                    return false;
                }
            }

            if (creature.Combat.RecentAttackers.Count() > 2) return false;

            if (Owner.Definition.ReactionType == ReactionType.CombatAggressive)
            {
                if (Owner.Definition.CombatLevel * 2 < character.Statistics.FullCombatLevel) return false;
            }

            return true;
        }

        /// <summary>
        /// Performs the aggressiveness check.
        /// </summary>
        public virtual void AggressivenessTick()
        {
            if (Owner.Combat.IsInCombat())
            {
                return;
            }

            if (Owner.Definition.ReactionType == ReactionType.Aggressive || Owner.Definition.ReactionType == ReactionType.CombatAggressive)
            {
                var characters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>();
                if (!characters.Any())
                {
                    return;
                }

                ICharacter? character = null;
                var minDistance = double.MaxValue;
                foreach (var t in characters.Where(IsAggressiveTowards))
                {
                    var distance = Owner.Location.GetDistance(t.Location);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        character = t;
                    }
                }

                if (character != null) Owner.Combat.SetTarget(character);
            }
        }

        /// <summary>
        /// Get's if this npc can be poisoned.
        /// By default this method checks if this npc is poisonable.
        /// </summary>
        /// <returns><c>true</c> if this instance can poison; otherwise, <c>false</c>.</returns>
        public virtual bool CanPoison() => Owner.Definition.Poisonable;

        /// <summary>
        /// Perform's attack on specific target.
        /// By default this method will perform a standart melee attack.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void PerformAttack(ICreature target)
        {
            RenderAttack();
            Owner.Combat.PerformAttack(new AttackParams
            {
                Target = target,
                DamageType = DamageType.StandardMelee,
                Damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target),
                MaxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target)
            });
        }

        /// <summary>
        /// Happens when the attack has been performed on the target.
        /// By default this method will do nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void OnAttackPerformed(ICreature target) { }

        /// <summary>
        /// Happens when attacker attack reaches the npc.
        /// By default this method does nothing and returns the damage provided in parameters.
        /// </summary>
        /// <param name="attacker">Creature which performed attack.</param>
        /// <param name="damageType">Type of the attack.</param>
        /// <param name="damage">Amount of damage inflicted on this character or -1 if it's a miss.</param>
        /// <returns>Return's amount of damage remains after defence.</returns>
        public virtual int OnAttack(ICreature attacker, DamageType damageType, int damage) => damage;

        /// <summary>
        /// Happens when attacker starts attack to this npc.
        /// By default this method does nothing and returns the damage provided in parameters.
        /// </summary>
        /// <param name="attacker">Creature which started the attack.</param>
        /// <param name="damageType">Type of the attack.</param>
        /// <param name="damage">Amount of damage that is predicted to be inflicted or -1 if it's a miss.</param>
        /// <param name="delay">Delay in client ticks until the attack will reach this npc and OnAttack will be called.</param>
        /// <returns>Return's amount of damage that remains after defence.</returns>
        public virtual int OnIncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay) => damage;

        /// <summary>
        /// Render's standart attack.
        /// This method is not guaranteed to render correct attack.
        /// By default, it is called by default implementation of PerformAttack method.
        /// </summary>
        public virtual void RenderAttack()
        {
            if (Owner.Definition.AttackAnimation != -1) Owner.QueueAnimation(Animation.Create(Owner.Definition.AttackAnimation));
            if (Owner.Definition.AttackGraphic != -1) Owner.QueueGraphic(Graphic.Create(Owner.Definition.AttackGraphic));
        }

        /// <summary>
        /// Render's defence of this npc.
        /// </summary>
        /// <param name="delay">Delay in client ticks till attack will reach the target.</param>
        public virtual void RenderDefence(int delay)
        {
            if (Owner.Definition.DefenceAnimation != -1) Owner.QueueAnimation(Animation.Create(Owner.Definition.DefenceAnimation, delay));
            if (Owner.Definition.DefenseGraphic != -1) Owner.QueueGraphic(Graphic.Create(Owner.Definition.DefenseGraphic, delay));
        }

        /// <summary>
        /// Called when [character click walk].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="canInteract">if set to <c>true</c> [can interact].</param>
        public virtual void OnCharacterClickReached(ICharacter clicker, NpcClickType clickType, bool canInteract)
        {
            if (canInteract)
                OnCharacterClickPerform(clicker, clickType);
            else
            {
                if (clicker.HasState<FrozenState>())
                    clicker.SendChatMessage(GameStrings.MagicalForceMovement);
                else
                    clicker.SendChatMessage(GameStrings.YouCantReachThat);
            }
        }

        /// <summary>
        /// Happens when this npc is clicked by specified character ('clicker').
        /// By default this method does for possible attack option.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        /// <param name="forceRun">Wheter clicker should forcerun to this npc.</param>
        public virtual void OnCharacterClick(ICharacter clicker, NpcClickType clickType, bool forceRun)
        {
            clicker.Interrupt(this);
            if (clickType == NpcClickType.Option2Click)
            {
                clicker.Movement.MovementType = clicker.Movement.MovementType == MovementType.Run || forceRun ? MovementType.Run : MovementType.Walk;
                clicker.FaceLocation(Owner);
                clicker.Combat.SetTarget(Owner);
            }
            else if (clickType == NpcClickType.Option6Click)
            {
                if (clicker.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                    clicker.SendChatMessage(Owner.ToString()!, ChatMessageType.ConsoleText);
                clicker.SendChatMessage(Owner.Definition.Examine);
            }
            else
            {
                if (clicker.EventManager.SendEvent(new WalkAllowEvent(clicker, Owner.Location, forceRun, false)))
                {
                    clicker.Movement.MovementType = clicker.Movement.MovementType == MovementType.Run || forceRun ? MovementType.Run : MovementType.Walk;
                    clicker.QueueTask(new CreatureReachTask(clicker, Owner, (success) => OnCharacterClickReached(clicker, clickType, success)));
                }
            }
        }

        /// <summary>
        /// Happens when character clicks NPC and then walks to it and reaches it.
        /// This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        /// handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public virtual void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option1Click)
            {
                var script = clicker.ServiceProvider.GetRequiredService<DefaultNpcDialogueScript>();
                clicker.Widgets.OpenDialogue(script, false, Owner);
            }
            else if (clicker.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                clicker.SendChatMessage("npc_click_perform[id=" + Owner.Definition.Id + ",loc=(" + Owner.Location + "),type=" + clickType + "]",
                    ChatMessageType.ConsoleText);
            else
                clicker.SendChatMessage(GameStrings.NothingInterestingHappens);
        }

        /// <summary>
        /// Determines whether this instance can respawn.
        /// If false, then the npc will be unregistered from the world.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can spawn; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanRespawn() => Owner.Definition.RespawnTime > 0;

        /// <summary>
        /// Determines whether this instance can spawn.
        /// If false, then the npc will be unregistered from the world.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can spawn; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanSpawn() => true;

        /// <summary>
        /// Get's if this npc can be destroyed.
        /// By default , this method returns true.
        /// </summary>
        /// <returns><c>true</c> if this instance can destroy; otherwise, <c>false</c>.</returns>
        public virtual bool CanDestroy() => true;

        /// <summary>
        /// Get's if this npc can be suspended.
        /// By default , this method returns true.
        /// </summary>
        /// <returns><c>true</c> if this instance can suspend; otherwise, <c>false</c>.</returns>
        public virtual bool CanSuspend() => true;

        /// <summary>
        /// Tick's npc.
        /// By default, this method does nothing.
        /// </summary>
        public virtual void Tick()
        {
            if (!Owner.Definition.Attackable)
            {
                return;
            }

            AggressivenessTick();
        }
    }
}