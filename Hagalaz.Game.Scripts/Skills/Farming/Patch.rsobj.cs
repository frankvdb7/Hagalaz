using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Farming
{
    /// <summary>
    /// </summary>
    public class Patch : GameObjectScript
    {
        private readonly IFarmingSkillService _farmingSkillService;

        public Patch(IFarmingSkillService farmingSkillService) => _farmingSkillService = farmingSkillService;

        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click || clickType == GameObjectClickType.Option2Click || clickType == GameObjectClickType.Option3Click || clickType == GameObjectClickType.Option4Click)
            {
                _farmingSkillService.HandlePatchClickPerform(clicker, Owner, clickType);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Uses the item on game object.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool UseItemOnGameObject(IItem used, ICharacter character)
        {
            if (_farmingSkillService.HandlePatchItem(character, used, Owner).Result)
            {
                return true;
            }

            return base.UseItemOnGameObject(used, character);
        }

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize() {}

        /// <summary>
        ///     Called when [rendered for].
        /// </summary>
        /// <param name="character">The character.</param>
        public override void OnRenderedFor(ICharacter character)
        {
            var patch = character.Farming.GetFarmingPatch(Owner.Id);
            patch?.Refresh();
        }
    }
}