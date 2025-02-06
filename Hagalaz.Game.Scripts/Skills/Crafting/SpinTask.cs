using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    /// <summary>
    /// </summary>
    public class SpinTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SpinTask" /> class.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="totalSpinCount">The total spin count.</param>
        public SpinTask(ICharacter performer, SpinDto definition, int totalSpinCount)
        {
            Performer = performer;
            Definition = definition;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            TotalSpinCount = totalSpinCount;
            _itemRepository = performer.ServiceProvider.GetRequiredService<IItemService>();
        }

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     The definition.
        /// </summary>
        private SpinDto Definition { get; }

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int SpinCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        private int TotalSpinCount { get; }

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemRepository;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (SpinCount == TotalSpinCount)
            {
                Cancel();
                return;
            }

            if (TickCount == 1 || TickCount % 6 == 0)
            {
                var resource = Performer.Inventory.GetById(Definition.ResourceID);
                if (resource == null)
                {
                    Performer.SendChatMessage("You need " + _itemRepository.FindItemDefinitionById(Definition.ResourceID).Name.ToLower() + " in order to create a " + _itemRepository.FindItemDefinitionById(Definition.ProductID).Name.ToLower() + ".");
                    Cancel();
                    return; 
                }

                Performer.QueueAnimation(Animation.Create(883));
                return;
            }

            if (TickCount % 3 == 0)
            {
                SpinCount++;
                var resource = Performer.Inventory.GetById(Definition.ResourceID);
                if (resource == null)
                {
                    Cancel();
                    return;
                }

                var slot = Performer.Inventory.GetInstanceSlot(resource);
                if (slot == -1)
                {
                    Cancel();
                    return;
                }

                Performer.Inventory.Replace(slot, new Item(Definition.ProductID));
                Performer.Statistics.AddExperience(StatisticsConstants.Crafting, Definition.CraftingExperience);
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