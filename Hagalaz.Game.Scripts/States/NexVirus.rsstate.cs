using System.Linq;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.States;

namespace Hagalaz.Game.Scripts.States
{
    /// <summary>
    /// </summary>
    [StateScriptMetaData(typeof(NexVirusState))]
    public class NexVirus : StateScriptBase
    {
        /// <summary>
        ///     Gets called when the state is added.
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public override void OnStateAdded(IState state, ICreature creature)
        {
            var character = creature as ICharacter;
            RsTickTask? virusTick = null;
            virusTick = new RsTickTask(() =>
            {
                if (RandomStatic.Generator.Next(0, 20) == 0)
                {
                    creature.RemoveState<NexVirusState>();
                    virusTick?.Cancel();
                    return;
                }

                if (virusTick?.TickCount == 1 || virusTick?.TickCount % 10 == 0)
                {
                    character?.Speak("*cough*");
                    DamageCombatSkills(character!, 1);
                    var characters = character.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => c.WithinRange(creature, 3));
                    foreach (var target in characters)
                    {
                        if (!target.HasState<NexVirusState>())
                        {
                            target.AddState(new NexVirusState()); // spread the virus
                        }
                    }
                }
            });
            character.QueueTask(virusTick);
        }

        /// <summary>
        ///     Damages the combat skills.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="damage">The damage.</param>
        private void DamageCombatSkills(ICharacter character, byte damage)
        {
            character.Statistics.DamageSkill(StatisticsConstants.Attack, damage);
            character.Statistics.DamageSkill(StatisticsConstants.Strength, damage);
            character.Statistics.DamageSkill(StatisticsConstants.Defence, damage);
            character.Statistics.DamageSkill(StatisticsConstants.Ranged, damage);
            character.Statistics.DamageSkill(StatisticsConstants.Magic, damage);
        }


        /// <summary>
        ///     Gets called when the state is removed.
        ///     By default, this method invokes the OnRemoveCallback of the state, if any.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        public override void OnStateRemoved(IState state, ICreature creature)
        {
            if (creature is ICharacter character)
            {
                character.SendChatMessage("You are cured from Nex' virus and are no longer coughing.");
            }
        }
    }
}