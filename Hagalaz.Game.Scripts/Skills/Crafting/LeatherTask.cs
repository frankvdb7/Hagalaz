using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;

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

        public LeatherTask(ICharacterContextAccessor characterContextAccessor, IItemService itemService, IItemBuilder itemBuilder)
        {
            _itemService = itemService;
            _itemBuilder = itemBuilder;
            Performer = characterContextAccessor.Context.Character;
            ;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = Performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
        }

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     The definition.
        /// </summary>
        public LeatherDto Definition { get; set; } = null!;

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int MakeCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        public int TotalMakeCount { get; set; }

        /// <summary>
        ///     Contains the thread count.
        /// </summary>
        private int ThreadCount { get; set; }

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
                    Performer.SendChatMessage("You need " + Definition.RequiredResourceCount + " x " +
                                              _itemService.FindItemDefinitionById(Definition.ResourceID).Name.ToLower() + " in order to create " +
                                              _itemService.FindItemDefinitionById(Definition.ProductId).Name.ToLower() + ".");
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
                    var tremoved = Performer.Inventory.Remove(_itemBuilder.Create().WithId(CraftingSkillService.ThreadId).Build());
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
                    var nremoved = Performer.Inventory.Remove(_itemBuilder.Create().WithId(CraftingSkillService.NeedleId).Build());
                    if (nremoved <= 0)
                    {
                        Cancel();
                        return;
                    }

                    Performer.SendChatMessage("Your needle has broken.");
                }

                var removed = Performer.Inventory.Remove(
                    _itemBuilder.Create().WithId(Definition.ResourceID).WithCount(Definition.RequiredResourceCount).Build());
                if (removed < Definition.RequiredResourceCount)
                {
                    Cancel();
                    return;
                }

                Performer.Inventory.Add(_itemBuilder.Create().WithId(Definition.ProductId).Build());
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