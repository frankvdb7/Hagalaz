using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Critters
{
    [NpcScriptMetaData([5149, 5161, 5162, 5163])]
    public class Sheep : NpcScriptBase
    {
        private readonly IAudioBuilder _audioBuilder;

        /// <summary>
        ///     The speak tick
        /// </summary>
        private int _speakTick;

        public Sheep(IAudioBuilder soundBuilder) => _audioBuilder = soundBuilder;

        /// <summary>
        ///     Happens when character clicks NPC and then walks to it and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        ///     handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option1Click)
            {
                if (RandomStatic.Generator.Next(0, 2) == 0)
                {
                    Owner.Speak("Baa!");
                    _audioBuilder.Create().AsSound().WithId(756).Build().PlayFor(clicker);
                    var task = new LocationReachTask(Owner, Owner.Location.Translate(RandomStatic.Generator.Next(-3, 3), RandomStatic.Generator.Next(-3, 3), 0), success =>
                    {
                        if (success)
                        {
                            Owner.ResetFacing();
                        }
                    });
                    Owner.QueueTask(task);
                    clicker.SendChatMessage("The sheep runs away from you.");
                }
                else
                {
                    if (clicker.Inventory.Contains(1735))
                    {
                        _audioBuilder.Create().AsSound().WithId(761).Build().PlayFor(clicker);
                        clicker.QueueAnimation(Animation.Create(893));
                        clicker.Inventory.Add(new Item(1737));
                        clicker.SendChatMessage("You shear the sheep of its fleece.");
                        Owner.Appearance.Transform(5149);
                        Owner.QueueTask(new RsTask(() =>
                            {
                                Owner.Appearance.Transform(Owner.Definition.Id);
                                Owner.QueueTask(new NpcSpawnPointReachTask(Owner, true));
                            }, 100));
                    }
                    else
                    {
                        clicker.SendChatMessage("You need a pair of shears to shear the sheep.");
                    }
                }

                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Tick's npc.
        ///     By default, this method does nothing.
        /// </summary>
        public override void Tick()
        {
            if (++_speakTick >= 5)
            {
                if (RandomStatic.Generator.Next(0, 8) == 0)
                {
                    Owner.Speak("Baa");
                }

                _speakTick = 0;
            }
        }
    }
}