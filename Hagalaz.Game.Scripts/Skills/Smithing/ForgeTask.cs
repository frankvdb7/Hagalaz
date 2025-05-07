using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Smithing
{
    /// <summary>
    /// </summary>
    public class ForgeTask : RsTickTask
    {
        public ForgeTask(ICharacterContextAccessor characterContextAccessor, IItemService itemService, IItemBuilder itemBuilder)
        {
            Performer = characterContextAccessor.Context.Character;
            TickActionMethod = PerformTickImpl;
            _itemService = itemService;
            _itemBuilder = itemBuilder;
            _interruptEvent = Performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
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
        public SmithingDefinition Definition { get; set; } = null!;

        /// <summary>
        ///     The definition.
        /// </summary>
        public ForgingBarEntry ForgeDefinition { get; set; } = null!;

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int ForgeCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        public int TotalForgeCount { get; set; }

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
            if (ForgeCount == TotalForgeCount)
            {
                Cancel();
                return;
            }

            if (TickCount == 1 || TickCount % 6 == 0)
            {
                if (!Performer.Inventory.Contains(Definition.BarID, ForgeDefinition.RequiredBarCount))
                {
                    Performer.SendChatMessage("You do not have sufficient " + _itemService.FindItemDefinitionById(Definition.BarID).Name.ToLower() + "s.");
                    Cancel();
                    return;
                }

                Performer.QueueAnimation(Animation.Create(898));
                //this.Performer.SendMessage("You place the required ores and attempt to create a bar of " + World.ItemsManager.GetItemDefinition(this.Definition.BarID).Name.ToLower().Replace(" bar", "") + ".");
                return;
            }

            if (TickCount % 3 != 0)
            {
                return;
            }

            ForgeCount++;
            var removed = Performer.Inventory.Remove(_itemBuilder.Create().WithId(Definition.BarID).WithCount(ForgeDefinition.RequiredBarCount).Build());
            if (removed < ForgeDefinition.RequiredBarCount) // something went wrong
            {
                Cancel();
                return;
            }

            // TODO message
            //this.Performer.SendMessage("You retrieve a bar of " + World.ItemsManager.GetItemDefinition(this.Definition.BarID).Name.ToLower().Replace(" bar", "") + ".");

            Performer.Inventory.Add(_itemBuilder.Create().WithId(ForgeDefinition.Product.Id).WithCount(ForgeDefinition.Product.Count).Build());
            Performer.Statistics.AddExperience(StatisticsConstants.Smithing,
                Definition.ForgeDefinition.BaseSmithingExperience * ForgeDefinition.RequiredBarCount); // Stop smithing if leveled up.
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