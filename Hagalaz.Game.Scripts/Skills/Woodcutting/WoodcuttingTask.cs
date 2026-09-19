using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Scripts.Skills.Woodcutting
{
    /// <summary>
    ///     Task for woodcutting.
    /// </summary>
    public class WoodcuttingTask : RsTickTask
    {
        /// <summary>
        ///     Construct's new firemaking task.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="finishCallback">The finish callback.</param>
        /// <param name="chance">The chance.</param>
        /// <param name="hatchetData">The hatchet data.</param>
        /// <param name="gameObjectHandle">The game object handle.</param>
        /// <param name="ivyTree">if set to <c>true</c> [ivy tree].</param>
        public WoodcuttingTask(
            ICharacter performer,
            Func<IGameObject, bool> finishCallback,
            double chance,
            HatchetDto hatchetData,
            EntityHandle<IGameObject> gameObjectHandle,
            bool ivyTree)
        {
            _performer = performer;
            _finishCallback = finishCallback;
            _entityService = performer.ServiceProvider.GetRequiredService<IEntityService>();
            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            _chance = chance;
            _hatchetData = hatchetData;
            _gameObjectHandle = gameObjectHandle;
            _ivyTree = ivyTree;
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;


        /// <summary>
        ///     Contains finish callback.
        /// </summary>
        internal readonly Func<IGameObject, bool> _finishCallback;

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private readonly ICharacter _performer;

        /// <summary>
        /// Resolves live world entities.
        /// </summary>
        private readonly IEntityService _entityService;

        /// <summary>
        ///     The chance of getting log from the tree.
        /// </summary>
        private readonly double _chance;

        /// <summary>
        ///     Information on the hatchet being vial.
        /// </summary>
        private readonly HatchetDto _hatchetData;

        /// <summary>
        ///     The object this task is for.
        /// </summary>
        private readonly EntityHandle<IGameObject> _gameObjectHandle;

        /// <summary>
        ///     Whether this tree is a ivy tree.
        /// </summary>
        private readonly bool _ivyTree;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (!_entityService.TryResolve(_gameObjectHandle, out var gameObject) || gameObject is null || gameObject.IsDisabled)
            {
                Cancel();
                return;
            }

            var randomValue = RandomStatic.Generator.NextDouble();
            if (randomValue <= _chance)
            {
                if (_finishCallback(gameObject))
                {
                    Cancel();
                    return;
                }
            }

            if (TickCount % 4 == 0)
            {
                _performer.QueueAnimation(Animation.Create(_ivyTree ? _hatchetData.CanoeAnimationId : _hatchetData.ChopAnimationId));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            _performer.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent!);
        }
    }
}
