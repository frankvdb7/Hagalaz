using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

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

        public SpinTask(ICharacterContextAccessor characterContextAccessor, IItemService itemService, IItemBuilder itemBuilder)
        {
            Performer = characterContextAccessor.Context.Character;
            ;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = Performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            _itemService = itemService;
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     The definition.
        /// </summary>
        public SpinDto Definition { get; set; } = null!;

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int SpinCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        public int TotalSpinCount { get; set; }

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemService;

        private readonly IItemBuilder _itemBuilder;

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
                    Performer.SendChatMessage("You need " + _itemService.FindItemDefinitionById(Definition.ResourceID).Name.ToLower() +
                                              " in order to create a " + _itemService.FindItemDefinitionById(Definition.ProductID).Name.ToLower() + ".");
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

                Performer.Inventory.Replace(slot, _itemBuilder.Create().WithId(Definition.ProductID).Build());
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