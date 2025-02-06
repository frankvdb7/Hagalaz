using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Woodcutting
{
    /// <summary>
    ///     Represents a tree.
    /// </summary>
    public class Tree : GameObjectScript
    {
        private readonly IWoodcuttingSkillService _woodcuttingSkillService;

        public Tree(IWoodcuttingSkillService woodcuttingSkillService)
        {
            _woodcuttingSkillService = woodcuttingSkillService;
        }

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                clicker.QueueTask(async () =>
                {
                    await _woodcuttingSkillService.StartCutting(clicker, Owner);
                });
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}