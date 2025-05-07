using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    /// <summary>
    /// </summary>
    public class CutGemTask : RsTickTask
    {
        private readonly IItemBuilder _itemBuilder;

        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened _interruptEvent;

        public CutGemTask(ICharacterContextAccessor characterContextAccessor, IItemBuilder itemBuilder)
        {
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
        public GemDto Definition { get; set; } = null!;

        /// <summary>
        ///     The times performed.
        /// </summary>
        private int CutCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        public int TotalCutCount { get; set; }

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

                Performer.Inventory.Replace(slot, _itemBuilder.Create().WithId(Definition.CutGemID).Build());
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