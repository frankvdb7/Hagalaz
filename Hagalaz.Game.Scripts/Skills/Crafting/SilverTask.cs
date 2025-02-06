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
    public class SilverTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened _interruptEvent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SilverTask" /> class.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="totalMakeCount">The total make count.</param>
        public SilverTask(ICharacter performer, SilverDto definition, int totalMakeCount)
        {
            Performer = performer;
            Definition = definition;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            TotalMakeCount = totalMakeCount;
            _itemRepository = performer.ServiceProvider.GetRequiredService<IItemService>();
        }

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     The definition.
        /// </summary>
        private SilverDto Definition { get; }

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int MakeCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        private int TotalMakeCount { get; }

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
            if (MakeCount == TotalMakeCount)
            {
                Cancel();
                return;
            }

            if (TickCount == 1 || TickCount % 6 == 0)
            {
                if (!Performer.Inventory.Contains(CraftingSkillService.SilverBar))
                {
                    Performer.SendChatMessage("You do not have any more " + _itemRepository.FindItemDefinitionById(CraftingSkillService.SilverBar).Name + " that you can use.");
                    Cancel();
                    return;
                }

                Performer.QueueAnimation(Animation.Create(3243));
                return;
            }

            if (TickCount % 3 == 0)
            {
                MakeCount++;
                var removed = Performer.Inventory.Remove(new Item(CraftingSkillService.SilverBar));
                if (removed <= 0)
                {
                    Cancel();
                    return;
                }

                Performer.Inventory.Add(new Item(Definition.ProductID));
                Performer.SendChatMessage("You shape the silver bar with the mould to make a " + _itemRepository.FindItemDefinitionById(Definition.ProductID).Name.ToLower() + ".");

                Performer.Statistics.AddExperience(StatisticsConstants.Crafting, Definition.Experience);
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