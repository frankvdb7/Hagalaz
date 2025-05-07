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
    public class SilverTask : RsTickTask
    {
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened _interruptEvent;

        public SilverTask(ICharacterContextAccessor characterContextAccessor, IItemBuilder itemBuilder, IItemService itemService)
        {
            _itemBuilder = itemBuilder;
            Performer = characterContextAccessor.Context.Character;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = Performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            _itemService = itemService;
        }

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     The definition.
        /// </summary>
        public SilverDto Definition { get; set; } = null!;

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int MakeCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        public int TotalMakeCount { get; set; }

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemService;

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
                    Performer.SendChatMessage("You do not have any more " + _itemService.FindItemDefinitionById(CraftingSkillService.SilverBar).Name +
                                              " that you can use.");
                    Cancel();
                    return;
                }

                Performer.QueueAnimation(Animation.Create(3243));
                return;
            }

            if (TickCount % 3 != 0)
            {
                return;
            }

            MakeCount++;
            var removed = Performer.Inventory.Remove(_itemBuilder.Create().WithId(CraftingSkillService.SilverBar).Build());
            if (removed <= 0)
            {
                Cancel();
                return;
            }

            Performer.Inventory.Add(_itemBuilder.Create().WithId(Definition.ProductID).Build());
            Performer.SendChatMessage("You shape the silver bar with the mould to make a " +
                                      _itemService.FindItemDefinitionById(Definition.ProductID).Name.ToLower() + ".");

            Performer.Statistics.AddExperience(StatisticsConstants.Crafting, Definition.Experience);
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