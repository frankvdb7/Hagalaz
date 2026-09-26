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

namespace Hagalaz.Game.Scripts.Skills.Mining
{
    /// <summary>
    ///     Task for mining.
    /// </summary>
    public class MiningTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;
        
        /// <summary>
        ///     Construct's new mining task.
        /// </summary>
        public MiningTask(
            ICharacter performer,
            Func<IGameObject, bool> finishCallback,
            double chance,
            PickaxeDto pickaxeData,
            EntityHandle<IGameObject> gameObjectHandle)
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
            _pickaxeData = pickaxeData;
            _gameObjectHandle = gameObjectHandle;
        }

        /// <summary>
        ///     Contains finish callback.
        /// </summary>
        private readonly Func<IGameObject, bool> _finishCallback;

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
        ///     Information on the hatchet.
        /// </summary>
        private readonly PickaxeDto _pickaxeData;

        /// <summary>
        ///     The object this task is for.
        /// </summary>
        private readonly EntityHandle<IGameObject> _gameObjectHandle;

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
                _performer.QueueAnimation(Animation.Create(_pickaxeData.AnimationId));
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
