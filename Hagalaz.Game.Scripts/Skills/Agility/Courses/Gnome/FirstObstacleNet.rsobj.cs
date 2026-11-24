using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome
{
    /// <summary>
    /// </summary>
    [GameObjectScriptMetaData([69383])]
    public class FirstObstacleNet : GameObjectScript
    {
        private readonly IAgilitySkillService _agilitySkillService;

        public FirstObstacleNet(IAgilitySkillService agilitySkillService) => _agilitySkillService = agilitySkillService;

        /// <summary>
        ///     The speak texts
        /// </summary>
        private static readonly string[] _speakTexts = ["Move it, move it, move it!"];

        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                clicker.Interrupt(this);
                clicker.Movement.Lock(true);
                clicker.SendChatMessage("You climb the netting...");
                clicker.QueueAnimation(Animation.Create(828));
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Teleport(Teleport.Create(Location.Create(clicker.Location.X, 3423, 1, clicker.Location.Dimension)));
                        _agilitySkillService.AddObstacleExperience(clicker, Owner.Id);
                        clicker.AddState(new GnomeCourseFirstObstacleNetState());
                        clicker.Movement.Unlock(false);
                    }, 2));
                _agilitySkillService.CheckGnomeCourseNpCs(clicker, _speakTexts[RandomStatic.Generator.Next(0, _speakTexts.Length)]);
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}
