using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Smithing
{
    /// <summary>
    /// 
    /// </summary>
    public class SmeltTask : RsTickTask
    {
        private readonly IItemBuilder _itemBuilder;

        public SmeltTask(ICharacterContextAccessor characterContextAccessor, IItemBuilder itemBuilder, IItemService itemService)
        {
            _itemBuilder = itemBuilder;
            Performer = characterContextAccessor.Context.Character;
            TickActionMethod = PerformTickImpl;
            _itemService = itemService;
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
        ///     The times performed.
        /// </summary>
        private int SmeltCount { get; set; }

        /// <summary>
        ///     The times to perform.
        /// </summary>
        public int TotalSmeltCount { get; set; }

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

                    Performer.SendChatMessage("You need " + _itemService.FindItemDefinitionById(ore.Id).Name.ToLower() + " to create a " +
                                              _itemService.FindItemDefinitionById(Definition.BarID).Name.ToLower() + ".");
                    Cancel();
                    return;
                }

                Performer.QueueAnimation(Animation.Create(897));
                Performer.SendChatMessage("You place the required ores and attempt to create a bar of " +
                                          _itemService.FindItemDefinitionById(Definition.BarID).Name.ToLower().Replace(" bar", "") + ".");
                return;
            }

            if (TickCount % 3 == 0)
            {
                SmeltCount++;
                var ores = Definition.SmeltDefinition.RequiredOres;
                var removed = ores.Sum(ore => Performer.Inventory.Remove(_itemBuilder.Create().WithId(ore.Id).WithCount(ore.Count).Build()));

                if (removed <= 0)
                {
                    Cancel();
                    return;
                }

                if (IsSuccess())
                {
                    Performer.SendChatMessage("You retrieve a bar of " +
                                              _itemService.FindItemDefinitionById(Definition.BarID).Name.ToLower().Replace(" bar", "") + ".");
                    Performer.Inventory.Add(_itemBuilder.Create().WithId(Definition.BarID).Build());
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