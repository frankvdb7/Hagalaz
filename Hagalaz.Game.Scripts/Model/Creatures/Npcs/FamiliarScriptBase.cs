using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Npcs.Familiars;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    /// Class Summoning NPC Script.
    /// Handles summoning of npcs and their special affects.
    /// </summary>
    public abstract class FamiliarScriptBase : NpcScriptBase, IFamiliarScript, IHydratable<HydratedFamiliar>, IDehydratable<HydratedFamiliar>
    {
        /// <summary>
        /// The amount of ticks left before this NPC gets removed from the world.
        /// </summary>
        private int _despawnTicks;

        /// <summary>
        /// The path finder
        /// </summary>
        private readonly IPathFinder _pathFinder;

        private readonly INpcService _npcService;
        private readonly IItemService _itemService;

        /// <summary>
        /// Sets the definition.
        /// </summary>
        /// <value>
        /// The definition.
        /// </value>
        public SummoningDto Definition { get; private set; }

        /// <summary>
        /// Contains the summoner.
        /// </summary>
        public ICharacter Summoner { get; private set; }

        /// <summary>
        /// Contains the owner of this script.
        /// </summary>
        public INpc Familiar => Owner;

        /// <summary>
        /// Gets the special move points.
        /// </summary>
        /// <value>
        /// The special move points.
        /// </value>
        public int SpecialMovePoints { get; private set; }

        /// <summary>
        /// Gets a value indicating whether [using special move].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [using special move]; otherwise, <c>false</c>.
        /// </value>
        public bool UsingSpecialMove { get; private set; }

        public FamiliarScriptBase(ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService)
        {
            _pathFinder = pathFinder;
            _npcService = npcService;
            _itemService = itemService;
        }

        /// <summary>
        /// Initializes the specified owner.
        /// </summary>
        /// <param name="summoner">The summoner.</param>
        /// <param name="definition">The definition.</param>
        public void InitializeSummoner(ICharacter summoner, SummoningDto definition)
        {
            Summoner = summoner;
            Definition = definition;
        }

        /// <summary>
        /// Get's called when owner is found.
        /// </summary>
        protected sealed override void Initialize()
        {
            SpecialMovePoints = 60;

            // Load event handlers.

            EventHappened sAllowHandler = null;
            sAllowHandler = Summoner.RegisterEventHandler(new EventHappened<SummoningAllowEvent>((e) =>
            {
                // Dissallow summoning.
                if (Summoner.HasFamiliar())
                {
                    Summoner.SendChatMessage("You can not spawn another familiar!");
                    return true;
                }

                Summoner.UnregisterEventHandler<SummoningAllowEvent>(sAllowHandler);
                return false;
            }));

            EventHappened sDiedHandler = null;
            sDiedHandler = Summoner.RegisterEventHandler(new EventHappened<CreatureDiedEvent>((e) =>
            {
                Owner.OnDeath();
                Summoner.UnregisterEventHandler<CreatureDiedEvent>(sDiedHandler);
                return false;
            }));

            EventHappened dismissHandler = null;
            dismissHandler = Summoner.RegisterEventHandler(new EventHappened<FamiliarDismissEvent>((e) =>
            {
                _npcService.UnregisterAsync(Owner);
                Summoner.UnregisterEventHandler<FamiliarDismissEvent>(dismissHandler);
                return false;
            }));

            EventHappened setTargetHandler = null;
            setTargetHandler = Summoner.RegisterEventHandler(new EventHappened<CreatureSetCombatTargetEvent>((e) =>
            {
                Owner.QueueTask(new RsTask(() => Owner.Combat.SetTarget(e.CombatTarget), 1));
                return false;
            }));

            InitializeFamiliar();
        }

        /// <summary>
        /// Initializes the familiar.
        /// </summary>
        protected virtual void InitializeFamiliar() {}

        /// <summary>
        /// Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            Owner.Combat.PerformAttack(new AttackParams
            {
                Target = target,
                DamageType = DamageType.FullSummoning,
                Damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target),
                MaxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target),
                HitType = HitSplatType.HitMeleeDamage,
            });
        }

        /// <summary>
        /// Get's called when npc is spawned.
        /// </summary>
        public override void OnSpawn()
        {
            ResetTimer();
            new FamiliarSpawnedEvent(Summoner, Owner).Send();
        }

        /// <summary>
        /// Calls the familiar.
        /// </summary>
        public void CallFamiliar()
        {
            Owner.Interrupt(this);
            Owner.Movement.Teleport(Teleport.Create(Summoner.Location.Clone()));
            OnTeleport();
        }

        /// <summary>
        /// Renews the familiar.
        /// </summary>
        public void RenewFamiliar()
        {
            if (!Summoner.Inventory.Contains(Definition.PouchId, 1))
            {
                Summoner.SendChatMessage("You do not have the required pouch to renew this familiar!");
                return;
            }

            var itemBuilder = Summoner.ServiceProvider.GetRequiredService<IItemBuilder>();
            Summoner.Inventory.Remove(itemBuilder.Create().WithId(Definition.PouchId).Build());
            ResetTimer();
            Summoner.SendChatMessage("Your familiar has been renewed.");
        }

        /// <summary>
        /// Get's called when npc is teleported.
        /// </summary>
        public virtual void OnTeleport() => Owner.QueueGraphic(Graphic.Create(Owner.Definition.Size > 1 ? 1315 : 1314));

        /// <summary>
        /// Faces the summoner.
        /// </summary>
        public void FaceSummoner() => Owner.FaceCreature(Summoner);

        /// <summary>
        /// Get's called when npc is destroyed permanently.
        /// </summary>
        public override void OnDestroy()
        {
            if (Summoner.IsDestroyed)
            {
                return;
            }

            Summoner.SendChatMessage("Your familiar vanished.");
        }

        /// <summary>
        /// Get's if this npc can aggro attack specific character.
        /// By default this method does check if creature is character.
        /// This method does not get called by ticks if npc reaction is not aggresive.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///   <c>true</c> if this instance can aggro the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsAggressiveTowards(ICreature creature) => false;

        /// <summary>
        /// Performs the aggressiveness check.
        /// </summary>
        public override void AggressivenessTick()
        {
            // Nothing
        }

        /// <summary>
        /// Determines whether this instance [can set target] the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns></returns>
        public override bool CanSetTarget(ICreature target) => target != Summoner && base.CanSetTarget(target);

        /// <summary>
        /// Get's if this npc can attack the specified character ('target').
        /// By default , this method returns true.
        /// </summary>
        /// <param name="target">Creature which is being attacked by this npc.</param>
        /// <returns>
        /// If attack can be performed.
        /// </returns>
        public override bool CanAttack(ICreature target) =>
            Owner.Viewport.VisibleCreatures.Contains(target) && Owner.Location.WithinDistance(target.Location, CreatureConstants.VisibilityDistance);

        /// <summary>
        /// Get's if this npc can be attacked by the specified character ('attacker').
        /// By default , this method does check if this npc is attackable.
        /// </summary>
        /// <param name="attacker">Creature which is attacking this npc.</param>
        /// <returns>
        /// If attack can be performed.
        /// </returns>
        public override bool CanBeAttackedBy(ICreature attacker) => attacker != Summoner;

        /// <summary>
        /// Get's if this npc can retaliate to specific character attack.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can retaliate to] the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanRetaliateTo(ICreature creature) => false;

        /// <summary>
        /// Get's attack bonus type of this npc.
        /// By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        /// AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Summoning;

        /// <summary>
        /// Get's if this npc can be suspended.
        /// By default , this method returns true.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can suspend; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanSuspend() => false;

        /// <summary>
        /// Get's if this npc can be destroyed.
        /// By default , this method returns true.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can destroy; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanDestroy() => false;

        /// <summary>
        /// Contains the route finder the NPC will use.
        /// By default, this method returns the dumb route finder when in combat
        /// and the dumb route finder for random walking.
        /// </summary>
        /// <returns></returns>
        public override IPathFinder GetPathfinder() => _pathFinder;

        /// <summary>
        /// Happens when character clicks NPC and then walks to it and reaches it.
        /// This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        /// handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option1Click && Summoner == clicker)
            {
                clicker.Interrupt(this);
                var script = clicker.ServiceProvider.GetRequiredService<StandardFamiliarDialogue>();
                clicker.Widgets.OpenDialogue(script, false, Owner);
            }
            else
                base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        /// Called when [set target].
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnSetTarget(ICreature target) => ((ICharacterCombat)Summoner.Combat).CheckSkullConditions(target);

        /// <summary>
        /// Called when [cancel target].
        /// By default, this method will let the Familiar walk to its summoner.
        /// </summary>
        public override void OnCancelTarget() => FollowSummoner();

        /// <summary>
        /// Checks the perform special move.
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckPerformSpecialMove()
        {
            if (SpecialMovePoints < GetRequiredSpecialMovePoints())
            {
                Summoner.SendChatMessage(GameStrings.NotEnoughSpecialMovePoints);
                SetUsingSpecialMove(false);
                return false;
            }

            var scroll = Summoner.Inventory.FirstOrDefault(i => i?.Id == Definition.ScrollId);
            if (scroll != null)
            {
                return true;
            }

            var def = _itemService.FindItemDefinitionById(Definition.ScrollId);
            Summoner.SendChatMessage("You need a " + def.Name + " in order to perform this special move!");
            SetUsingSpecialMove(false);
            return false;
        }

        /// <summary>
        /// Specials the move clicked.
        /// </summary>
        public virtual bool SpecialMoveClicked()
        {
            if (UsingSpecialMove)
                SetUsingSpecialMove(false);
            else
            {
                if (CheckPerformSpecialMove())
                {
                    SetUsingSpecialMove(true);
                    Summoner.QueueAnimation(Animation.Create(7660));
                    Summoner.QueueGraphic(Graphic.Create(1316));
                }
            }

            return UsingSpecialMove;
        }

        /// <summary>
        /// Performs the special attack.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void PerformSpecialMove(IRuneObject target)
        {
            if (Summoner.Inventory.Remove(new Item(Definition.ScrollId, 1)) >= 1)
            {
                DrainSpecialMovePoints(GetRequiredSpecialMovePoints());
                SetUsingSpecialMove(false);

                Summoner.Statistics.AddExperience(StatisticsConstants.Summoning, Definition.ScrollExperience);
            }
        }

        /// <summary>
        /// Sets the special move target.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void SetSpecialMoveTarget(IRuneObject? target) { }

        /// <summary>
        /// Get's amount of special move points required by this familiar.
        /// By default , this method does throw NotImplementedException
        /// </summary>
        /// <returns>System.Int16.</returns>
        public virtual int GetRequiredSpecialMovePoints() => 0;

        /// <summary>
        /// Gets the name of the special move.
        /// </summary>
        /// <returns>The special move name</returns>
        public virtual string GetSpecialMoveName() => string.Empty;

        /// <summary>
        /// Gets the special attack description.
        /// </summary>
        /// <returns>The special attack description</returns>
        public virtual string GetSpecialMoveDescription() => string.Empty;

        /// <summary>
        /// Gets the type of the special.
        /// </summary>
        /// <returns>SpecialType</returns>
        public virtual FamiliarSpecialType GetSpecialType() => FamiliarSpecialType.Click;

        /// <summary>
        /// Drain's character special energy.
        /// </summary>
        /// <param name="amount">Amount of special energy to be drained.</param>
        /// <returns>Return's the actual amount of energy drained.</returns>
        public int DrainSpecialMovePoints(int amount)
        {
            if (amount > SpecialMovePoints) amount = SpecialMovePoints;
            SetSpecialMovePoints(SpecialMovePoints - amount);
            return amount;
        }

        /// <summary>
        /// Set's using special move boolean.
        /// </summary>
        /// <param name="isUsing">if set to <c>true</c> [is using].</param>
        public void SetUsingSpecialMove(bool isUsing)
        {
            UsingSpecialMove = isUsing;
            RefreshUsingSpecialAttack();
        }

        /// <summary>
        /// Refreshes special attack boolean in client.
        /// </summary>
        public static void RefreshUsingSpecialAttack()
        {
            //TODO - Send the correct configurations.
        }

        /// <summary>
        /// Set's familiar special move points.
        /// </summary>
        /// <param name="amount">The amount.</param>
        public void SetSpecialMovePoints(int amount)
        {
            SpecialMovePoints = amount;
            RefreshSpecialMovePoints();
        }

        /// <summary>
        /// Heals the special move points.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="max">The max.</param>
        /// <returns></returns>
        public int HealSpecialMovePoints(int amount, int max)
        {
            if (SpecialMovePoints + amount > max) amount = max - SpecialMovePoints;
            if (amount < 0) amount = 0;
            SetSpecialMovePoints(SpecialMovePoints + amount);
            return amount;
        }

        /// <summary>
        /// Refreshes familiar special move points.
        /// </summary>
        public void RefreshSpecialMovePoints() => Summoner.Configurations.SendStandardConfiguration(1177, SpecialMovePoints);

        /// <summary>
        /// Resets the timer.
        /// </summary>
        public void ResetTimer()
        {
            _despawnTicks = Definition.Ticks;
            RefreshTimer();
        }

        /// <summary>
        /// Refreshes the timer.
        /// Every value of 75 is 30 seconds.
        /// </summary>
        public void RefreshTimer() => Summoner.Configurations.SendStandardConfiguration(1176, _despawnTicks * 600 / 1000 / 30 * 65);

        /// <summary>
        /// Tick's npc.
        /// </summary>
        public override void Tick()
        {
            _despawnTicks--;
            if (_despawnTicks <= 0)
            {
                new FamiliarDismissEvent(Summoner).Send();
                return;
            }

            if (_despawnTicks == 100)
            {
                Summoner.SendChatMessage("You have 1 minute remaining before your familiar vanishes.");
            }
            else if (_despawnTicks == 50)
            {
                Summoner.SendChatMessage("You have 30 seconds remaining before your familiar vanishes.");
            }

            if (_despawnTicks % 50 == 0)
            {
                HealSpecialMovePoints(15, 60);
                RefreshTimer();
            }

            if (_despawnTicks % 100 == 0)
            {
                Summoner.Statistics.DamageSkill(StatisticsConstants.Summoning, 1);
            }

            FollowTick();
        }

        /// <summary>
        /// Following tick.
        /// </summary>
        private void FollowTick()
        {
            if (Owner.Combat.IsDead) return;
            if (Summoner.IsDestroyed)
            {
                _npcService.UnregisterAsync(Owner);
                return;
            }

            if (!Owner.Viewport.VisibleCreatures.Contains(Summoner))
            {
                CallFamiliar();
                return;
            }

            if (Owner.Combat.IsInCombat()) return;
            FollowSummoner();
        }

        /// <summary>
        /// Follows the summoner.
        /// </summary>
        private void FollowSummoner()
        {
            if (Owner.FacedCreature != Summoner) FaceSummoner();

            Owner.Movement.MovementType = Summoner.Movement.MovementType;

            var path = _pathFinder.Find(Owner, Summoner, true);
            if (!path.Successful && !path.MovedNear)
            {
                CallFamiliar();
                return;
            }

            if (path.ReachedDestination)
            {
                return;
            }

            Owner.Movement.AddToQueue(path);
        }

        public void Hydrate(HydratedFamiliar hydration)
        {
            SetSpecialMovePoints(hydration.SpecialMovePoints);
            _despawnTicks = hydration.TicksRemaining;
            SetUsingSpecialMove(hydration.IsUsingSpecialMove);
            RefreshTimer();
        }

        public HydratedFamiliar Dehydrate() =>
            new()
            {
                IsUsingSpecialMove = UsingSpecialMove, SpecialMovePoints = SpecialMovePoints, TicksRemaining = _despawnTicks
            };
    }
}