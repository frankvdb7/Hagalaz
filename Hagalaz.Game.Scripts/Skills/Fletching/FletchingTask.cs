using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    /// <summary>
    /// </summary>
    public class FletchingTask : RsTickTask
    {
        /// <summary>
        ///     Construct's new cooking task.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="productIndex">Index of the product.</param>
        /// <param name="totalFletchCount">The total cook count.</param>
        /// <param name="onFletchPerformCallback">The on fletch perform callback.</param>
        /// <param name="tickDelay">The tick delay.</param>
        public FletchingTask(ICharacter performer, FletchingDefinition definition, int productIndex, int totalFletchCount, Func<int, bool> onFletchPerformCallback, int tickDelay)
        {
            Performer = performer;
            Definition = definition;
            ProductIndex = productIndex;
            OnFletchPerformCallback = onFletchPerformCallback;
            TickDelay = tickDelay;
            TotalFletchCount = totalFletchCount;

            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
        }


        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     The definition.
        /// </summary>
        private FletchingDefinition Definition { get; }

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int FletchCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        private int TotalFletchCount { get; }

        /// <summary>
        ///     Gets the index of the product.
        /// </summary>
        /// <value>
        ///     The index of the product.
        /// </value>
        private int ProductIndex { get; }

        /// <summary>
        ///     Gets the on fletch perform callback.
        /// </summary>
        /// <value>
        ///     The on fletch perform callback.
        /// </value>
        private Func<int, bool> OnFletchPerformCallback { get; }

        /// <summary>
        ///     Gets the tick delay.
        /// </summary>
        /// <value>
        ///     The tick delay.
        /// </value>
        private int TickDelay { get; }

        /// <summary>
        ///     Performs the animation.
        /// </summary>
        private void PerformAnimation()
        {
            if (Definition.AnimationID != -1)
            {
                Performer.QueueAnimation(Animation.Create(Definition.AnimationID));
            }
        }

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (FletchCount == TotalFletchCount)
            {
                Cancel();
                return;
            }

            if (TickCount == 1)
            {
                PerformAnimation();
            }

            if (TickCount % TickDelay == 0)
            {
                FletchCount++;
                if (FletchCount != TotalFletchCount)
                {
                    PerformAnimation();
                }

                if (OnFletchPerformCallback(ProductIndex))
                {
                    Cancel();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            Performer.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent!);
        }
    }
}