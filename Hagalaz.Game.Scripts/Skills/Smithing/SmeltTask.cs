using System.Linq;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Smithing
{
    /// <summary>
    /// 
    /// </summary>
    public class SmeltTask : RsTickTask
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SmeltTask" /> class.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="totalSmeltCount">The total smelt count.</param>
        public SmeltTask(ICharacter performer, SmithingDefinition definition, int totalSmeltCount)
        {
            Performer = performer;
            Definition = definition;
            TickActionMethod = PerformTickImpl;
            TotalSmeltCount = totalSmeltCount;
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
        ///     The times performed.
        /// </summary>
        private int SmeltCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        private int TotalSmeltCount { get; }

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
            if (SmeltCount == TotalSmeltCount)
            {
                Cancel();
                return;
            }

            if (TickCount == 1 || TickCount % 6 == 0)
            {
                var ores = Definition.SmeltDefinition.RequiredOres;
                foreach (var ore in ores)
                {
                    if (Performer.Inventory.Contains(ore.Id, ore.Count))
                    {
                        continue;
                    }

                    Performer.SendChatMessage("You need " + _itemRepository.FindItemDefinitionById(ore.Id).Name.ToLower() + " to create a " + _itemRepository.FindItemDefinitionById(Definition.BarID).Name.ToLower() + ".");
                    Cancel();
                    return;
                }

                Performer.QueueAnimation(Animation.Create(897));
                Performer.SendChatMessage("You place the required ores and attempt to create a bar of " + _itemRepository.FindItemDefinitionById(Definition.BarID).Name.ToLower().Replace(" bar", "") + ".");
                return;
            }

            if (TickCount % 3 == 0)
            {
                SmeltCount++;
                var ores = Definition.SmeltDefinition.RequiredOres;
                var removed = ores.Sum(ore => Performer.Inventory.Remove(new Item(ore.Id, ore.Count)));

                if (removed <= 0)
                {
                    Cancel();
                    return;
                }
                
                if (IsSuccess())
                {
                    Performer.SendChatMessage("You retrieve a bar of " + _itemRepository.FindItemDefinitionById(Definition.BarID).Name.ToLower().Replace(" bar", "") + ".");
                    Performer.Inventory.Add(new Item(Definition.BarID));
                    Performer.Statistics.AddExperience(StatisticsConstants.Smithing, Definition.SmeltDefinition.SmithingExperience);
                }
                else
                {
                    Performer.SendChatMessage("The ore is too impure and you fail to refine it.");
                }
            }
        }

        /// <summary>
        ///     Determines whether this instance is succes.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance is succes; otherwise, <c>false</c>.
        /// </returns>
        private bool IsSuccess()
        {
            // iron ore
            if (Definition.BarID == 2351)
            {
                return RandomStatic.Generator.Next(0, 100) <= (Performer.Statistics.GetSkillLevel(StatisticsConstants.Smithing) >= 45 ? 80 : 50);
            }

            return true;
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