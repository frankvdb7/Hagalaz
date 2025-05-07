using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Crafting
{
    /// <summary>
    /// </summary>
    [GameObjectScriptMetaData([2642, 34801])]
    public class PottersWheel : GameObjectScript
    {
        private readonly ICraftingSkillService _craftingSkillService;

        public PottersWheel(ICraftingSkillService craftingSkillService) => _craftingSkillService = craftingSkillService;

        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                clicker.QueueTask(() => _craftingSkillService.TryFormPottery(clicker));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}