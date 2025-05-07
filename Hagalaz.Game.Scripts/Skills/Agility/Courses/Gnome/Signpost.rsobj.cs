using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome
{
    [GameObjectScriptMetaData([69514])]
    public class Signpost : GameObjectScript
    {
        private readonly IMovementBuilder _movementBuilder;

        /// <summary>
        ///     The speak texts
        /// </summary>
        private static readonly string[] _speakTexts = ["Come on! I'd be over there by now"];

        public Signpost(IMovementBuilder movementBuilder) => _movementBuilder = movementBuilder;

        /// <summary>
        ///     Called when [character click].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="forceRun">if set to <c>true</c> [force run].</param>
        public override void OnCharacterClick(ICharacter clicker, GameObjectClickType clickType, bool forceRun)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (!new WalkAllowEvent(clicker, Owner.Location, forceRun, false).Send())
                {
                    return;
                }

                clicker.ForceRunMovementType(forceRun);
                var task = new GameObjectReachTask(clicker, Owner, success =>
                {
                    if (success || Owner.Location.WithinDistance(clicker.Location, 1))
                    {
                        //clicker.TurnTo(-1, -1); // reset turn to
                        OnCharacterClickPerform(clicker, clickType);
                    }
                    else
                    {
                        clicker.SendChatMessage(clicker.HasState(StateType.Frozen) ? GameStrings.MagicalForceMovement : GameStrings.YouCantReachThat);
                    }
                });
                clicker.QueueTask(task);

                return;
            }

            base.OnCharacterClick(clicker, clickType, forceRun);
        }

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
                clicker.QueueAnimation(Animation.Create(2922));
                var movement = _movementBuilder
                    .Create()
                    .WithStart(clicker.Location)
                    .WithEnd(Location.Create(2484, 3418, 3, clicker.Location.Dimension))
                    .WithStartSpeed(30)
                    .WithEndSpeed(90)
                    .Build();
                clicker.QueueForceMovement(movement);
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Teleport(Teleport.Create(Location.Create(2484, 3418, 3, clicker.Location.Dimension)));
                        clicker.Statistics.AddExperience(StatisticsConstants.Agility, 25);
                        clicker.AddState(new State(StateType.GnomeCourseSignpost, int.MaxValue));
                        clicker.Movement.Unlock(false);
                    }, 4));
                Agility.CheckGnomeCourseNpCs(clicker, _speakTexts[RandomStatic.Generator.Next(0, _speakTexts.Length)]);
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