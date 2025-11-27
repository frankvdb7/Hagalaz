using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Magic.MiscSpells
{
    /// <summary>
    ///     Contains low level alchemy script.
    /// </summary>
    public class LowLevelAlchemy : ILowLevelAlchemy
    {
        private readonly IItemBuilder _itemBuilder;
        private readonly ICharacter _caster;

        /// <summary>
        /// </summary>
        private static readonly RuneType[] _runes = [RuneType.Fire, RuneType.Nature];

        /// <summary>
        /// </summary>
        private static readonly int[] _runeAmounts = [3, 1];

        public LowLevelAlchemy(IItemBuilder itemBuilder, ICharacterContextAccessor characterContextAccessor)
        {
            _itemBuilder = itemBuilder;
            _caster = characterContextAccessor.Context.Character;
        }

        /// <summary>
        ///     Casts the spell.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public bool Cast(IItem item)
        {
            if (_caster.HasState<AlchingState>())
            {
                return false;
            }

            if (!CheckRequirements(_caster))
            {
                return false;
            }

            var slot = _caster.Inventory.GetInstanceSlot(item);
            if (slot == -1)
            {
                return false;
            }

            var coins = _itemBuilder.Create().WithId(995).WithCount(item.ItemDefinition.LowAlchemyValue).Build();
            if (!_caster.Inventory.HasSpaceFor(coins) && !_caster.MoneyPouch.HasSpaceFor(coins))
            {
                _caster.SendChatMessage(GameStrings.InventoryFull);
                return false;
            }

            RemoveRequirements(_caster);
            var removed = _caster.Inventory.Remove(_itemBuilder.Create().WithId(item.Id).WithCount(1).Build(), slot);
            if (removed <= 0)
            {
                return true;
            }

            if (!_caster.Inventory.Add(coins))
            {
                return true;
            }

            _caster.QueueAnimation(Animation.Create(712));
            _caster.QueueGraphic(Graphic.Create(112));
            _caster.Statistics.AddExperience(StatisticsConstants.Magic, 31);
            _caster.Configurations.SendGlobalCs2Int(168, 7); // set active tab.
            _caster.AddState(new AlchingState { TicksLeft = 2 });
            return true;
        }

        /// <summary>
        ///     Checks the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        public static bool CheckRequirements(ICharacter caster) => caster.Magic.CheckMagicLevel(21) && caster.Magic.CheckRunes(_runes, _runeAmounts);

        /// <summary>
        ///     Removes the requirements.
        /// </summary>
        /// <param name="caster">The caster.</param>
        public static void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(_runes, _runeAmounts);
    }
}