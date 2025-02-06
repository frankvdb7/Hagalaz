using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;

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
        public MiningTask(ICharacter performer, Func<ValueTask<bool>> finishCallback, double chance, PickaxeDto pickaxeData, IGameObject gameObject)
        {
            _performer = performer;
            _finishCallback = finishCallback;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            _chance = chance;
            _pickaxeData = pickaxeData;
            _gameObject = gameObject;
        }

        /// <summary>
        ///     Contains finish callback.
        /// </summary>
        private readonly Func<ValueTask<bool>> _finishCallback;

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private readonly ICharacter _performer;

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
        private readonly IGameObject _gameObject;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private async void PerformTickImpl()
        {
            var randomValue = RandomStatic.Generator.NextDouble();
            if (randomValue <= _chance)
            {
                if (_gameObject.IsDestroyed || _gameObject.IsDisabled)
                {
                    Cancel();
                    return;
                }

                if (await _finishCallback())
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