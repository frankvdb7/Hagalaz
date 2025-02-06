using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.GrandExchange.GameObjects
{
    /// <summary>
    /// </summary>
    public class UnderwallTunnel : GameObjectScript
    {
        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (clicker.Statistics.GetSkillLevel(StatisticsConstants.Agility) < 21)
                {
                    clicker.SendChatMessage("You need an agility level of 21 to use this shortcut.");
                    return;
                }

                var destination = Owner.Id == 9311
                    ? Location.Create(3143, 3514, 0, clicker.Location.Dimension)
                    : Location.Create(3139, 3516, 0, clicker.Location.Dimension);
                var between = Location.Create(3141, 3515, 0, clicker.Location.Dimension);

                clicker.Movement.AddToQueue(Owner.Location.Clone());
                clicker.QueueAnimation(Animation.Create(2589));
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Teleport(Teleport.Create(between));
                        clicker.QueueAnimation(Animation.Create(2590));
                    }, 2));

                var deltaX = Owner.Id == 9312 ? (short)RandomStatic.Generator.Next(-1, 0) : (short)RandomStatic.Generator.Next(0, 2);
                var deltaY = Owner.Id == 9312 ? (short)RandomStatic.Generator.Next(0, 2) : (short)RandomStatic.Generator.Next(-1, 0);
                switch (Owner.Id)
                {
                    case 9311:
                    {
                        if (deltaX == 0 && deltaY == 0)
                        {
                            deltaX = -1;
                        }

                        break;
                    }
                    case 9312:
                    {
                        if (deltaX == 0 && deltaY == 0)
                        {
                            deltaX = 1;
                        }

                        break;
                    }
                }

                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Teleport(Teleport.Create(destination));
                        clicker.QueueAnimation(Animation.Create(2591));
                        clicker.Movement.AddToQueue(destination.Translate(deltaX, deltaY, 0));
                    }, 4));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Gets the suitable objects.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [9311, 9312];

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}