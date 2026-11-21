using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects
{
    /// <summary>
    ///     A Godwars faction altar. Option 1 restores prayer points, at most once per
    ///     <typeparamref name="TPrayedState"/> timer and never in combat; Option 2 teleports to the faction's destination.
    /// </summary>
    /// <typeparam name="TPrayedState">The timer that stops the character from praying at this altar again for a while.</typeparam>
    public abstract class GodwarsAltarScript<TPrayedState> : GameObjectScript where TPrayedState : TimedState, new()
    {
        private const int PrayerPointsRestored = 990;
        private const int PrayerCooldownTicks = 1000;
        private const int AltarPlane = 2;

        /// <summary>
        ///     Where Option 2 teleports to, on the Godwars plane.
        /// </summary>
        protected abstract (int X, int Y) TeleportDestination { get; }

        /// <inheritdoc />
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            switch (clickType)
            {
                case GameObjectClickType.Option1Click:
                    Pray(clicker);
                    return;
                case GameObjectClickType.Option2Click:
                    new StandardTeleportScript(MagicBook.StandardBook, TeleportDestination.X, TeleportDestination.Y, AltarPlane, 0).PerformTeleport(clicker);
                    return;
                default:
                    base.OnCharacterClickPerform(clicker, clickType);
                    return;
            }
        }

        private static void Pray(ICharacter clicker)
        {
            if (clicker.HasState<TPrayedState>())
            {
                clicker.SendChatMessage("You cannot pray at this altar yet.");
                return;
            }

            if (clicker.Combat.IsInCombat())
            {
                return;
            }

            if (clicker.Statistics.HealPrayerPoints(PrayerPointsRestored) == 0)
            {
                clicker.SendChatMessage("You cannot restore any more prayer points.");
                return;
            }

            clicker.QueueAnimation(Animation.Create(645));
            clicker.SendChatMessage("You prayed to the gods and they restored your prayer points.");
            clicker.AddState(new TPrayedState { TicksLeft = PrayerCooldownTicks });
        }
    }
}
