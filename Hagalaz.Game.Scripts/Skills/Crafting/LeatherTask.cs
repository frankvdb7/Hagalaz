using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    /// <summary>
    /// </summary>
    public class LeatherTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened _interruptEvent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LeatherTask" /> class.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="totalCraftCount">The total craft count.</param>
        public LeatherTask(ICharacter performer, LeatherDto definition, int totalCraftCount)
        {
            Performer = performer;
            Definition = definition;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            TotalMakeCount = totalCraftCount;
            _itemRepository = performer.ServiceProvider.GetRequiredService<IItemService>();
        }

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     The definition.
        /// </summary>
        private LeatherDto Definition { get; }

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int MakeCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        private int TotalMakeCount { get; }

        /// <summary>
        ///     Contains the thread count.
        /// </summary>
        private int ThreadCount { get; set; }

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
                if (Performer.Inventory.GetById(CraftingSkillService.NeedleId) == null)
                {
                    Performer.SendChatMessage("You need a needle to craft leathers.");
                    Cancel();
                    return;
                }

                if (Performer.Inventory.GetById(CraftingSkillService.ThreadId) == null)
                {
                    Performer.SendChatMessage("You need a thread to craft leathers.");
                    Cancel();
                    return;
                }

                if (Performer.Inventory.GetCountById(Definition.ResourceID) < Definition.RequiredResourceCount)
                {
                    Performer.SendChatMessage("You need " + Definition.RequiredResourceCount + " x " + _itemRepository.FindItemDefinitionById(Definition.ResourceID).Name.ToLower() + " in order to create " + _itemRepository.FindItemDefinitionById(Definition.ProductId).Name.ToLower() + ".");
                    Cancel();
                    return; 
                }

                Performer.QueueAnimation(Animation.Create(1249));
                return;
            }

            if (TickCount % 3 == 0)
            {
                MakeCount++;
                if (++ThreadCount == 5)
                {
                    var tremoved = Performer.Inventory.Remove(new Item(CraftingSkillService.ThreadId));
                    if (tremoved <= 0)
                    {
                        Cancel();
                        return;
                    }

                    Performer.SendChatMessage("You use up a reel of your thread.");
                    ThreadCount = 0;
                }

                if (RandomStatic.Generator.Next(0, 30) <= 3)
                {
                    var nremoved = Performer.Inventory.Remove(new Item(CraftingSkillService.NeedleId));
                    if (nremoved <= 0)
                    {
                        Cancel();
                        return;
                    }

                    Performer.SendChatMessage("Your needle has broken.");
                }

                var removed = Performer.Inventory.Remove(new Item(Definition.ResourceID, Definition.RequiredResourceCount));
                if (removed < Definition.RequiredResourceCount)
                {
                    Cancel();
                    return;
                }

                Performer.Inventory.Add(new Item(Definition.ProductId));
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