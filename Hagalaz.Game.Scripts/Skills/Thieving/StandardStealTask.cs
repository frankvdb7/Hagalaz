using System.Linq;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Tasks;

namespace Hagalaz.Game.Scripts.Skills.Thieving
{
    /// <summary>
    ///     Action for stealing.
    /// </summary>
    public class StandardStealTask : RsTask
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="StandardStealTask" /> class.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="gameObject">The gameObject.</param>
        /// <param name="stall">The stall.</param>
        /// <param name="executeDelay">The executing tick.</param>
        public StandardStealTask(ICharacter performer, IGameObject gameObject, Stall stall, int executeDelay)
        {
            _performer = performer;
            _gameObject = gameObject;
            ExecuteDelay = executeDelay;
            _stall = stall;
            ExecuteHandler = OnPerform;
            OnInitialized();
        }

        /// <summary>
        ///     Contains the performer.
        /// </summary>
        private readonly ICharacter _performer;

        /// <summary>
        ///     Contains the stall.
        /// </summary>
        private readonly IGameObject _gameObject;

        /// <summary>
        ///     The stall script.
        /// </summary>
        private readonly Stall _stall;

        /// <summary>
        ///     Called when [initialized].
        /// </summary>
        public virtual void OnInitialized()
        {
            _performer.QueueAnimation(Animation.Create(Thieving.PickPocketingAnimation));
            _performer.SendChatMessage("You attempt to steal the " + _gameObject.Name.ToLower() + "'s loot...");
        }

        /// <summary>
        ///     Called when [perform].
        /// </summary>
        public virtual void OnPerform()
        {
            _performer.RemoveState(StateType.ThievingStall); //always remove the state, otherwise the character can not steal again.
            if (_gameObject.IsDestroyed)
            {
                return;
            }

            // check if someone else has stole from this stall.
            var objRegion = _gameObject.Region;
            var obj = objRegion.FindStandardGameObject(_gameObject.Location.RegionLocalX, _gameObject.Location.RegionLocalY, _gameObject.Location.Z);
            if (obj == null || obj.IsDestroyed || obj.Id != _gameObject.Id)
            {
                return;
            }

            var guards = _performer.Viewport.VisibleCreatures.OfType<INpc>().Where(n => n.Name.Contains("Guard") && n.WithinRange(_performer, 3));
            foreach (var guard in guards)
            {
                if (RandomStatic.Generator.Next(0, 5) != 2)
                {
                    continue;
                }

                PerformStun(guard);
                return;
            }

            var lootService = _performer.ServiceProvider.GetRequiredService<ILootService>();
            var table = lootService.FindGameObjectLootTable(_gameObject.Definition.LootTableId).Result;
            if (table != null)
            {
                _performer.Inventory.TryAddLoot(_performer, table, out _);
            }

            _performer.SendChatMessage("You successfully stole the " + _gameObject.Name.ToLower() + "'s loot.");

            _performer.Statistics.AddExperience(StatisticsConstants.Thieving, _stall.Definition.Experience);

            if (_stall.Definition.EmptyGameObjectID == -1)
            {
                return;
            }

            if (_gameObject.Id == _stall.Definition.EmptyGameObjectID)
            {
                return;
            }

            var scheduler = _performer.ServiceProvider.GetRequiredService<IRsTaskService>();
            var gameObjectService = _performer.ServiceProvider.GetRequiredService<IGameObjectService>();
            var oldID = _gameObject.Id;
            gameObjectService.UpdateGameObject(new GameObjectUpdate
            {
                Instance = _gameObject,
                Id = _stall.Definition.EmptyGameObjectID
            });
            scheduler.Schedule(new RsTask(() =>
            {
                gameObjectService.UpdateGameObject(new GameObjectUpdate
                {
                    Instance = _gameObject,
                    Id = oldID
                });
            }, _stall.Definition.RespawnTicks));
        }

        /// <summary>
        ///     Performs the stun.
        /// </summary>
        /// <param name="guard">The guard.</param>
        private void PerformStun(INpc guard)
        {
            guard.Speak("Hey! Get your hands off there!");
            var task = new CreatureReachTask(guard,
                _performer,
                success =>
                {
                    if (!success)
                    {
                        return;
                    }

                    guard.QueueAnimation(Animation.Create(guard.Definition.AttackAnimation));
                    _performer.QueueAnimation(Animation.Create(Thieving.StunnedAnimation));
                    _performer.QueueGraphic(Graphic.Create(80, 0, 60));
                    _performer.Stun(6); // 4 sec

                    var splatBuilder = guard.ServiceProvider.GetRequiredService<IHitSplatBuilder>();
                    var splat = splatBuilder.Create()
                        .AddSprite(builder => builder.WithDamage(20)
                        .WithSplatType(HitSplatType.HitSimpleDamage))
                        .Build();
                    _performer.QueueHitSplat(splat);
                    _performer.Statistics.DamageLifePoints(20);
                });
            guard.QueueTask(task);
        }
    }
}