using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Minigames.Crucible.Interfaces;

namespace Hagalaz.Game.Scripts.Minigames.Crucible
{
    /// <summary>
    ///     TODO: Make this instanced.
    /// </summary>
    public static class Crucible
    {
        /// <summary>
        /// </summary>
        public class Fissure
        {
            /// <summary>
            ///     Gets the location.
            /// </summary>
            /// <value>
            ///     The location.
            /// </value>
            public ILocation Location { get; set; }

            /// <summary>
            ///     Gets the component identifier.
            /// </summary>
            /// <value>
            ///     The component identifier.
            /// </value>
            public short ComponentID { get; set; }
        }

        /// <summary>
        ///     The banks
        /// </summary>
        public static readonly Fissure[] Banks =
        [
            new()
            {
                Location = Location.Create(3209, 6144, 0), ComponentID = 4
            },
            new()
            {
                Location = Location.Create(3263, 6198, 0), ComponentID = 5
            },
            new()
            {
                Location = Location.Create(3318, 6144, 0), ComponentID = 6
            },
            new()
            {
                Location = Location.Create(3260, 6089, 0), ComponentID = 7
            }
        ];

        /// <summary>
        ///     The fissures
        /// </summary>
        public static readonly Fissure[] Fissures =
        [
            new()
            {
                Location = Location.Create(3266, 6132, 0), ComponentID = 8
            },
            new()
            {
                Location = Location.Create(3294, 6118, 0), ComponentID = 9
            },
            new()
            {
                Location = Location.Create(3279, 6151, 0), ComponentID = 10
            },
            new()
            {
                Location = Location.Create(3287, 6173, 0), ComponentID = 11
            },
            new()
            {
                Location = Location.Create(3259, 6183, 0), ComponentID = 12
            },
            new()
            {
                Location = Location.Create(3248, 6155, 0), ComponentID = 13
            },
            new()
            {
                Location = Location.Create(3230, 6144, 0), ComponentID = 14
            },
            new()
            {
                Location = Location.Create(3227, 6116, 0), ComponentID = 15
            },
            new()
            {
                Location = Location.Create(3259, 6100, 0), ComponentID = 16
            }
        ];

        /// <summary>
        ///     Teleports to fissure.
        /// </summary>
        /// <param name="character">The character.</param>
        public static void TeleportToFissure(ICharacter character) =>
            TeleportToFissure(character, Fissures[RandomStatic.Generator.Next(0, Fissures.Length)]);

        /// <summary>
        ///     Teleports to fissure.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="fissure">The fissure.</param>
        public static void TeleportToFissure(ICharacter character, Fissure fissure)
        {
            var teleport = () => { character.Movement.Teleport(Teleport.Create(fissure.Location)); };
            if (character.Area.Id != 29) // if in a banking area
            {
                var bountyInterfaceScript = character.ServiceProvider.GetRequiredService<BountyInterfaceScript>();
                bountyInterfaceScript.OnAcceptClicked = () => { teleport(); };
                character.Widgets.OpenWidget(1298,
                    0,
                    bountyInterfaceScript,
                    true);
                return;
            }

            teleport();
        }

        /// <summary>
        ///     Teleports to bank.
        /// </summary>
        /// <param name="character">The character.</param>
        public static void TeleportToBank(ICharacter character) =>
            character.Movement.Teleport(Teleport.Create(Banks[RandomStatic.Generator.Next(0, Banks.Length)].Location));
    }
}