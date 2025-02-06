using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Smithing
{
    /// <summary>
    /// </summary>
    public class ForgeTask : RsTickTask
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SmeltTask" /> class.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="forgeDefinition">The forge definition.</param>
        /// <param name="totalForgeCount">The total smelt count.</param>
        public ForgeTask(ICharacter performer, SmithingDefinition definition, ForgingBarEntry forgeDefinition, int totalForgeCount)
        {
            Performer = performer;
            Definition = definition;
            ForgeDefinition = forgeDefinition;
            TickActionMethod = PerformTickImpl;
            TotalForgeCount = totalForgeCount;
            _itemRepository = performer.ServiceProvider.GetRequiredService<IItemService>();
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
        private SmithingDefinition Definition { get; }

        /// <summary>
        ///     The definition.
        /// </summary>
        private ForgingBarEntry ForgeDefinition { get; }

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int ForgeCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        private int TotalForgeCount { get; }

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
            if (ForgeCount == TotalForgeCount)
            {
                Cancel();
                return;
            }

            if (TickCount == 1 || TickCount % 6 == 0)
            {
                if (!Performer.Inventory.Contains(Definition.BarID, ForgeDefinition.RequiredBarCount))
                {
                    Performer.SendChatMessage("You do not have sufficient " + _itemRepository.FindItemDefinitionById(Definition.BarID).Name.ToLower() + "s.");
                    Cancel();
                    return;
                }

                Performer.QueueAnimation(Animation.Create(898));
                //this.Performer.SendMessage("You place the required ores and attempt to create a bar of " + World.ItemsManager.GetItemDefinition(this.Definition.BarID).Name.ToLower().Replace(" bar", "") + ".");
                return;
            }

            if (TickCount % 3 == 0)
            {
                ForgeCount++;
                var removed = Performer.Inventory.Remove(new Item(Definition.BarID, ForgeDefinition.RequiredBarCount));
                if (removed < ForgeDefinition.RequiredBarCount) // something went wrong
                {
                    Cancel();
                    return;
                }

                // TODO message
                //this.Performer.SendMessage("You retrieve a bar of " + World.ItemsManager.GetItemDefinition(this.Definition.BarID).Name.ToLower().Replace(" bar", "") + ".");


                Performer.Inventory.Add(new Item(ForgeDefinition.Product.Id, ForgeDefinition.Product.Count));
                Performer.Statistics.AddExperience(StatisticsConstants.Smithing, Definition.ForgeDefinition.BaseSmithingExperience * ForgeDefinition.RequiredBarCount); // Stop smithing if leveled up.
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