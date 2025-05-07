using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Areas.Wilderness
{
    /// <summary>
    /// </summary>
    public class WildernessScript : CharacterScriptBase
    {
        private readonly IItemBuilder _itemBuilder;

        public WildernessScript(ICharacterContextAccessor contextAccessor, IItemBuilder itemBuilder) : base(contextAccessor) => _itemBuilder = itemBuilder;

        /// <summary>
        ///     Called when the character is killed by a creature.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnKilledBy(ICreature creature)
        {
            if (creature is not ICharacter killer || !Character.Combat.CanBeLootedBy(creature))
            {
                return;
            }

            killer.Mediator.Publish(new ProfileIncrementIntAction(ProfileConstants.PvpKillCount, 1));
            Character.Mediator.Publish(new ProfileIncrementIntAction(ProfileConstants.PvpDeathCount, 1));

            var honorTokens = _itemBuilder.Create().WithId(19864).WithCount(CalculateHonorTokensCount(killer)).Build();

            if (killer.Inventory.Add(honorTokens))
            {
                killer.SendChatMessage("<col=00BFFF>You earned " + honorTokens.Count + " x " + honorTokens.Name + "s!");
            }
        }

        /// <summary>
        ///     Calculates the honor tokens count.
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns></returns>
        private int CalculateHonorTokensCount(ICharacter killer)
        {
            // TODO - Calculate based on killed combat and networth
            var baseTokens = 1;
            // if killer is lower combat, reward more tokens
            if (killer.Statistics.BaseCombatLevel < Character.Statistics.BaseCombatLevel)
            {
                baseTokens += 4;
            }

            return baseTokens;
        }

        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}