using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.GameObjects.Cannon
{
    /// <summary>
    ///     Dwarf multi cannon object script.
    /// </summary>
    [GameObjectScriptMetaData([6, 29406, 29408])]
    public class DwarfMultiCannonGameObjectScript : GameObjectScript
    {
        /// <summary>
        ///     The cannon object ids.
        /// </summary>
        public static readonly int[] CannonObjectIds = [6, 7, 8, 9, 29406, 29398, 29401, 29402, 29408, 29403, 29404, 29405];

        /// <summary>
        ///     The cannon animation ids.
        /// </summary>
        public static readonly int[] CannonAnimationIds = [305, 307, 289, 184, 182, 178, 291, 303];

        /// <summary>
        ///     The cannon ball id.
        /// </summary>
        public const int CannonballItemID = 2;

        /// <summary>
        ///     The current direction
        /// </summary>
        private int _currentDirection;

        /// <summary>
        ///     The cannon owner
        /// </summary>
        private ICharacter _cannonOwner;

        /// <summary>
        ///     Wether the cannon is firing.
        /// </summary>
        private bool _firing;

        /// <summary>
        ///     The cannon balls
        /// </summary>
        private IItem _cannonBalls;

        /// <summary>
        ///     The fire delay.
        /// </summary>
        private int _fireDelay;

        /// <summary>
        ///     The projectile path finder
        /// </summary>
        private readonly IProjectilePathFinder _projectilePathFinder;

        private readonly IRsTaskService _rsTaskService;
        private readonly IHitSplatBuilder _hitSplatBuilder;
        private readonly IProjectileBuilder _projectileBuilder;
        private readonly IItemBuilder _itemBuilder;
        private readonly IGameObjectService _gameObjectService;

        public DwarfMultiCannonGameObjectScript(
            IProjectilePathFinder projectilePathFinder, IRsTaskService rsTaskService, IHitSplatBuilder hitSplatBuilder, IProjectileBuilder projectileBuilder,
            IItemBuilder itemBuilder, IGameObjectService gameObjectService)
        {
            _projectilePathFinder = projectilePathFinder;
            _rsTaskService = rsTaskService;
            _hitSplatBuilder = hitSplatBuilder;
            _projectileBuilder = projectileBuilder;
            _itemBuilder = itemBuilder;
            _gameObjectService = gameObjectService;
        }

        /// <summary>
        ///     Creates the specified cannon owner.
        /// </summary>
        /// <param name="cannonOwner">The cannon owner.</param>
        /// <returns></returns>
        public static DwarfMultiCannonGameObjectScript Create(ICharacter cannonOwner)
        {
            var script = cannonOwner.ServiceProvider.GetRequiredService<DwarfMultiCannonGameObjectScript>();
            script.SetCannonOwner(cannonOwner);
            return script;
        }

        /// <summary>
        ///     Initializes the specified script owner.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public void SetCannonOwner(ICharacter owner) => _cannonOwner = owner;

        /// <summary>
        ///     Happens when character click's this object and then walks to it
        ///     and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        ///     than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (_cannonOwner == clicker && clickType == GameObjectClickType.Option1Click)
            {
                AddCannonBalls();
                if (CanFire())
                {
                    _firing = true;
                }

                return;
            }

            if (_cannonOwner == clicker && clickType == GameObjectClickType.Option2Click)
            {
                _firing = false;
                PickupCannon(clicker, Owner, _cannonBalls);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Determines whether this instance can be destroyed.
        ///     By default, this method returns true.
        /// </summary>
        /// <returns></returns>
        public override bool CanDestroy() => false;

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
            _cannonBalls = _itemBuilder.Create().WithId(CannonballItemID).Build();
            StartRotating();
        }

        /// <summary>
        ///     Adds the cannon balls.
        /// </summary>
        private void AddCannonBalls()
        {
            var toRemoveCount = 30 - _cannonBalls.Count;
            var removed = _cannonOwner.Inventory.Remove(_itemBuilder.Create().WithId(CannonballItemID).WithCount(toRemoveCount).Build());
            _cannonBalls = _itemBuilder.Create().WithId(CannonballItemID).WithCount(_cannonBalls.Count + removed).Build();
            _cannonOwner.SendChatMessage("You load the cannon with " + removed + " cannonball" + (removed == 1 ? "" : "s") + ".");
        }

        /// <summary>
        ///     Determines whether this instance can fire.
        /// </summary>
        private bool CanFire()
        {
            if (_cannonBalls.Count != 0)
            {
                return Owner.Location.WithinDistance(_cannonOwner.Location, CreatureConstants.VisibilityDistance);
            }

            _cannonOwner.SendChatMessage("You do not have enough cannonballs to fire this cannon.");
            return false;
        }

        /// <summary>
        ///     Starts the firing.
        /// </summary>
        private void Fire()
        {
            if (!CanFire())
            {
                _firing = false;
                return;
            }

            var centerLoc = Owner.Location.Translate(1, 1, 0);
            var npcs = _cannonOwner.Viewport.VisibleCreatures.OfType<INpc>()
                .Where(n => centerLoc.WithinDistance(n.Location, 8) && _cannonOwner.Combat.CanSetTarget(n) && n.Combat.CanSetTarget(_cannonOwner));

            foreach (var npc in npcs)
            {
                var hit = false;
                var fromX = -1;
                var fromY = -1;
                var toX = -1;
                var toY = -1;
                npc.Viewport.GetLocalPosition(centerLoc, ref fromX, ref fromY);
                npc.Viewport.GetLocalPosition(npc.Location, ref toX, ref toY);

                if (!_projectilePathFinder.Find(centerLoc, npc.Location, false).Successful)
                {
                    continue;
                }

                var direction = centerLoc.GetDirection(npc.Location);

                if (_currentDirection == 0 && direction == DirectionFlag.NorthEast || _currentDirection == 1 && direction == DirectionFlag.East ||
                    _currentDirection == 2 && direction == DirectionFlag.SouthEast || _currentDirection == 3 && direction == DirectionFlag.South ||
                    _currentDirection == 4 && direction == DirectionFlag.SouthWest || _currentDirection == 5 && direction == DirectionFlag.West ||
                    _currentDirection == 6 && direction == DirectionFlag.NorthWest || _currentDirection == 7 && direction == DirectionFlag.North)
                {
                    hit = true;
                }

                if (!hit)
                {
                    continue;
                }

                var combat = (ICharacterCombat)_cannonOwner.Combat;

                var delay = Math.Max(10, (int)Location.GetDistance(Owner.Location.X + 1, Owner.Location.Y + 1, npc.Location.X, npc.Location.Y) * 5);

                _projectileBuilder.Create()
                    .WithGraphicId(53)
                    .FromLocation(centerLoc)
                    .ToCreature(npc)
                    .WithDuration(delay)
                    .WithDelay(32)
                    .WithFromHeight(10)
                    .WithToHeight(40)
                    .Send();

                const int max = 300;
                var preDmg = combat.GetRangedDamage(npc, false);
                //combat.PerformSoulSplit(victim, preDMG);
                preDmg = npc.Combat.IncomingAttack(_cannonOwner, DamageType.Standard, preDmg, delay + 30);

                _cannonOwner.Statistics.AddExperience(StatisticsConstants.Ranged, preDmg * 0.1 * 2.0);
                _cannonOwner.Statistics.AddExperience(StatisticsConstants.Constitution, preDmg * 0.1 * 1.33);

                _cannonBalls.Count -= 1;
                if (_cannonBalls.Count == 0)
                {
                    _cannonOwner.SendChatMessage("Your cannon has ran out of ammo!");
                }

                npc.QueueTask(new RsTask(() =>
                    {
                        var soaked = -1;
                        var damage = npc.Combat.Attack(_cannonOwner, DamageType.Standard, preDmg, ref soaked);
                        var splat = _hitSplatBuilder.Create()
                            .AddSprite(builder => builder.WithDamage(damage).WithSplatType(HitSplatType.HitCannonDamage).WithMaxDamage(max))
                            .Build();
                        npc.QueueHitSplat(splat);
                    },
                    CreatureHelper.CalculateTicksForClientTicks(delay + 30)));
                return;
            }
        }


        /// <summary>
        ///     Increments the current direction.
        /// </summary>
        private void IncrementCurrentDirection()
        {
            _currentDirection++;
            if (_currentDirection > 7)
            {
                _currentDirection = 0;
            }
        }

        /// <summary>
        ///     Starts the rotating.
        /// </summary>
        private void StartRotating()
        {
            RsTickTask task = null!;
            _rsTaskService.Schedule(task = new RsTickTask(() =>
            {
                if (Owner.IsDestroyed)
                {
                    task.Cancel();
                    return;
                }

                if (_cannonOwner.IsDestroyed || !_cannonOwner.HasState<CannonPlacedState>())
                {
                    Owner.Region.Remove(Owner);
                    task.Cancel();
                    return;
                }

                if (!_firing)
                {
                    return;
                }

                _fireDelay++;
                if (_fireDelay >= 4)
                {
                    Fire();
                }

                _gameObjectService.AnimateGameObject(Owner, Animation.Create(CannonAnimationIds[_currentDirection]));
                IncrementCurrentDirection();
            }));
        }

        public void PickupCannon(ICharacter character, IGameObject cannon, IItem cannonBalls)
        {
            if (!character.HasState<CannonPlacedState>())
            {
                return;
            }

            var cannonId = GetCannonIdByGameObject(cannon.Id);
            if (cannonId == -1)
            {
                return;
            }

            character.Movement.Lock(true);
            character.QueueAnimation(Animation.Create(827));

            var tick = 0;
            RsTickTask task = null!;

            character.QueueTask(task = new RsTickTask(() =>
            {
                if (tick == 6)
                {
                    character.SendChatMessage(GameStrings.PickCannonUp);
                    var toAdd = _itemBuilder.Create().WithId(DwarfMultiCannonItemScript.CannonItemIds[0 + cannonId]).Build();
                    if (!character.Inventory.HasSpaceFor(toAdd))
                    {
                        character.SendChatMessage(GameStrings.InventoryFull);
                        task.Cancel();
                        return;
                    }

                    character.Inventory.Add(toAdd);

                    if (!character.Inventory.HasSpaceFor(cannonBalls))
                    {
                        character.SendChatMessage(GameStrings.InventoryFull);
                        task.Cancel();
                        return;
                    }

                    character.Inventory.Add(cannonBalls);

                    character.Region.Remove(cannon);

                    character.RemoveState<CannonPlacedState>();

                    character.Movement.Unlock(false);
                    task.Cancel();
                    return;
                }

                if (tick == 4)
                {
                    character.SendChatMessage("You pick up the stand...");

                    var toAdd = _itemBuilder.Create().WithId(DwarfMultiCannonItemScript.CannonItemIds[1 + cannonId]).Build();
                    if (!character.Inventory.HasSpaceFor(toAdd))
                    {
                        character.SendChatMessage(GameStrings.InventoryFull);
                        character.Movement.Unlock(false);
                        task.Cancel();
                        return;
                    }

                    character.Inventory.Add(toAdd);

                    _gameObjectService.UpdateGameObject(new GameObjectUpdate
                    {
                        Instance = cannon, Id = CannonObjectIds[1 + cannonId]
                    });
                }
                else if (tick == 2)
                {
                    character.SendChatMessage("You pick up the barrel...");

                    var toAdd = _itemBuilder.Create().WithId(DwarfMultiCannonItemScript.CannonItemIds[2 + cannonId]).Build();
                    if (!character.Inventory.HasSpaceFor(toAdd))
                    {
                        character.SendChatMessage(GameStrings.InventoryFull);
                        character.Movement.Unlock(false);
                        task.Cancel();
                        return;
                    }

                    character.Inventory.Add(toAdd);

                    _gameObjectService.UpdateGameObject(new GameObjectUpdate
                    {
                        Instance = cannon, Id = CannonObjectIds[2 + cannonId]
                    });
                }
                else if (tick == 0)
                {
                    character.SendChatMessage("You pick up the furnace...");

                    var toAdd = _itemBuilder.Create().WithId(DwarfMultiCannonItemScript.CannonItemIds[3 + cannonId]).Build();
                    if (!character.Inventory.HasSpaceFor(toAdd))
                    {
                        character.SendChatMessage(GameStrings.InventoryFull);
                        character.Movement.Unlock(false);
                        task.Cancel();
                        return;
                    }

                    character.Inventory.Add(toAdd);

                    _gameObjectService.UpdateGameObject(new GameObjectUpdate
                    {
                        Instance = cannon, Id = CannonObjectIds[3 + cannonId]
                    });
                }

                character.QueueAnimation(Animation.Create(827));
                tick++;
            }));
        }

        private static int GetCannonIdByGameObject(int objectId)
        {
            if (objectId == CannonObjectIds[0])
            {
                return 0; // Normal cannon
            }

            if (objectId == CannonObjectIds[4])
            {
                return 4; // 4 - Gold cannon
            }

            if (objectId == CannonObjectIds[8])
            {
                return 8; // 8 - Royal cannon
            }

            return -1;
        }

        /// <summary>
        ///     Called when [use item].
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <returns></returns>
        public override bool UseItemOnGameObject(IItem used, ICharacter character)
        {
            if (used.Id != CannonballItemID)
            {
                return false;
            }

            AddCannonBalls();
            return true;
        }
    }
}