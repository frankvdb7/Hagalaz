using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    /// <summary>
    /// </summary>
    public class CutGemTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened _interruptEvent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CutGemTask" /> class.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="totalCutCount">The total cut count.</param>
        public CutGemTask(ICharacter performer, GemDto definition, int totalCutCount)
        {
            Performer = performer;
            Definition = definition;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            TotalCutCount = totalCutCount;
        }

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     The definition.
        /// </summary>
        private GemDto Definition { get; }

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int CutCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        private int TotalCutCount { get; }

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (CutCount == TotalCutCount)
            {
                Cancel();
                return;
            }

            if (TickCount == 1 || TickCount % 6 == 0)
            {
                Performer.QueueAnimation(Animation.Create(Definition.AnimationID));
                return;
            }

            if (TickCount % 1 == 0)
            {
                CutCount++;
                var uncutItem = Performer.Inventory.GetById(Definition.UncutGemID);
                if (uncutItem == null)
                {
                    Cancel();
                    return;
                }

                var slot = Performer.Inventory.GetInstanceSlot(uncutItem);
                if (slot == -1)
                {
                    Cancel();
                    return;
                }

                Performer.Inventory.Replace(slot, new Item(Definition.CutGemID));
                Performer.Statistics.AddExperience(StatisticsConstants.Crafting, Definition.CraftingExperience);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            Performer.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent);
        }
    }
}