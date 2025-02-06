using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    /// <summary>
    /// </summary>
    [GameObjectScriptMetaData([CraftingSkillService.GoldBar])]
    public class GoldBar : ItemScript
    {
        private readonly IServiceProvider _serviceProvider;

        public GoldBar(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        ///     Uses the item on game object.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedOn">The used on.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool UseItemOnGameObject(IItem used, IGameObject usedOn, ICharacter character)
        {
            if (usedOn.Id == 26814)
            {
                character.Widgets.OpenWidget(446, 0, _serviceProvider.GetRequiredService<JewelryScreen>(), true);
                return true;
            }

            return false;
        }
    }
}