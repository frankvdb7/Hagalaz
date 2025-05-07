using Hagalaz.Collections.Extensions;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Jewelry
{
    /// <summary>
    /// </summary>
    public static class Jewelry
    {
        /// <summary>
        ///     The glory teleports.
        /// </summary>
        public static readonly TeleportStruct[] GloryTeleports =
        [
            new TeleportStruct(Location.Create(3100, 3499, 0, 0), 0), // edgeville
            new TeleportStruct(Location.Create(2936, 3144, 0, 0), 3), // karamja
            new TeleportStruct(Location.Create(3093, 3244, 0, 0), 2), // draynor village
            new TeleportStruct(Location.Create(3293, 3174, 0, 0), 3) // al-kharid
        ];

        /// <summary>
        ///     The game necklace teleports.
        /// </summary>
        public static readonly TeleportStruct[] GameNecklaceTeleports =
        [
            new TeleportStruct(Location.Create(2970, 9679, 0, 0), 0), // gamer's grotto
            new TeleportStruct(Location.Create(2885, 4372, 2, 0), 0) // corporeal beast
        ];

        /// <summary>
        ///     The ring of dueling teleports.
        /// </summary>
        public static readonly TeleportStruct[] RingOfDuelingTeleports =
        [
            new TeleportStruct(Location.Create(3363, 3276, 0, 0), 2), // duel arena
            new TeleportStruct(Location.Create(2440, 3088, 0, 0), 2), // castle wars
            new TeleportStruct(Location.Create(2411, 2832, 0, 0), 2), // moblising armies
            new TeleportStruct(Location.Create(1697, 5604, 0, 0), 2) // fist of guthix
        ];

        /// <summary>
        ///     The ring of slaying teleports
        /// </summary>
        public static readonly TeleportStruct[] RingOfSlayingTeleports =
        [
            new TeleportStruct(Location.Create(3360, 2993, 0, 0), 0), // sumona
            new TeleportStruct(Location.Create(3429, 3538, 0, 0), 0), // slayer tower
            new TeleportStruct(Location.Create(2793, 3615, 0, 0), 2), // fremenik slayer dungeon
            new TeleportStruct(Location.Create(3185, 4598, 0, 0), 3) // tarn's Lair
        ];

        /// <summary>
        ///     Teleports the amulet of glory.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="jewelry">The glory.</param>
        /// <param name="equipment">if set to <c>true</c> [equipment].</param>
        /// <param name="teleport">The teleport.</param>
        public static void TeleportAmuletOfGlory(ICharacter character, IItem jewelry, bool equipment, TeleportStruct teleport) =>
            new JewelryTeleport(teleport.Location,
                teleport.TeleportDistance,
                () =>
                {
                    IContainer<IItem?> items = equipment ? character.Equipment : character.Inventory;
                    var slot = items.IndexOf(i => i == jewelry);
                    if (slot == -1)
                    {
                        return false;
                    }

                    var newItem = character.ServiceProvider.GetRequiredService<IItemBuilder>()
                        .Create()
                        .WithId(jewelry.Id - 2)
                        .WithCount(jewelry.Count)
                        .Build();
                    if (equipment)
                        character.Equipment.Replace((EquipmentSlot)slot, newItem);
                    else
                        character.Inventory.Replace(slot, newItem);

                    var nameArray = jewelry.Name.Split('(');
                    int.TryParse(nameArray[1].Replace("(", "").Replace(")", ""), out var charges);
                    if (charges != -1)
                    {
                        character.SendChatMessage("Your " + nameArray[0] + " has " + (charges - 1) + " charges remaining.");
                    }
                    else
                    {
                        character.SendChatMessage("Your " + nameArray[0] + " has been depleted of all its charges.");
                    }

                    return true;
                }).PerformTeleport(character);

        /// <summary>
        ///     Teleports the games necklace.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="jewelry">The jewelry.</param>
        /// <param name="equipment">if set to <c>true</c> [equipment].</param>
        /// <param name="teleport">The teleport.</param>
        public static void TeleportGamesNecklace(ICharacter character, IItem jewelry, bool equipment, TeleportStruct teleport) =>
            new JewelryTeleport(teleport.Location,
                teleport.TeleportDistance,
                () =>
                {
                    IContainer<IItem?> items = equipment ? character.Equipment : character.Inventory;
                    var slot = items.IndexOf(i => i == jewelry);
                    if (slot == -1)
                    {
                        return false;
                    }

                    if (jewelry.Id == 3867)
                    {
                        if (equipment)
                            character.Equipment.Remove(jewelry, EquipmentSlot.Amulet);
                        else
                            character.Inventory.Remove(jewelry, slot);
                        character.SendChatMessage("Your " + jewelry.Name + " has been depleted of all its charges.");
                    }
                    else
                    {
                        var newItem = character.ServiceProvider.GetRequiredService<IItemBuilder>()
                            .Create()
                            .WithId(jewelry.Id + 2)
                            .WithCount(jewelry.Count)
                            .Build();
                        if (equipment)
                            character.Equipment.Replace((EquipmentSlot)slot, newItem);
                        else
                            character.Inventory.Replace(slot, newItem);
                    }

                    var nameArray = jewelry.Name.Split('(');
                    int.TryParse(nameArray[1].Replace("(", "").Replace(")", ""), out var charges);
                    if (charges != -1)
                    {
                        character.SendChatMessage("Your " + nameArray[0] + " has " + (charges - 1) + " charges remaining.");
                    }
                    else
                    {
                        character.SendChatMessage("Your " + nameArray[0] + " has been depleted of all its charges.");
                    }

                    return true;
                }).PerformTeleport(character);

        /// <summary>
        ///     Teleports the ring of dueling.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="jewelry">The jewelry.</param>
        /// <param name="equipment">if set to <c>true</c> [equipment].</param>
        /// <param name="teleport">The teleport.</param>
        public static void TeleportRingOfDueling(ICharacter character, IItem jewelry, bool equipment, TeleportStruct teleport) =>
            new JewelryTeleport(teleport.Location,
                teleport.TeleportDistance,
                () =>
                {
                    IContainer<IItem?> items = equipment ? character.Equipment : character.Inventory;
                    var slot = items.IndexOf(i => i == jewelry);
                    if (slot == -1)
                    {
                        return false;
                    }

                    var newItem = character.ServiceProvider.GetRequiredService<IItemBuilder>()
                        .Create()
                        .WithId(jewelry.Id + 2)
                        .WithCount(jewelry.Count)
                        .Build();
                    if (equipment)
                        character.Equipment.Replace((EquipmentSlot)slot, newItem);
                    else
                        character.Inventory.Replace(slot, newItem);
                    var nameArray = jewelry.Name.Split('(');
                    int.TryParse(nameArray[1].Replace("(", "").Replace(")", ""), out var charges);
                    if (charges != -1)
                    {
                        character.SendChatMessage("Your " + nameArray[0] + " has " + (charges - 1) + " charges remaining.");
                    }
                    else
                    {
                        character.SendChatMessage("Your " + nameArray[0] + " has been depleted of all its charges.");
                    }

                    return true;
                }).PerformTeleport(character);

        /// <summary>
        ///     Teleports the ring of slaying.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="jewelry">The jewelry.</param>
        /// <param name="equipment">if set to <c>true</c> [equipment].</param>
        /// <param name="teleport">The teleport.</param>
        public static void TeleportRingOfSlaying(ICharacter character, IItem jewelry, bool equipment, TeleportStruct teleport) =>
            new JewelryTeleport(teleport.Location,
                teleport.TeleportDistance,
                () =>
                {
                    IContainer<IItem?> items = equipment ? character.Equipment : character.Inventory;
                    var slot = items.IndexOf(i => i == jewelry);
                    if (slot == -1)
                    {
                        return false;
                    }

                    var nameArray = jewelry.Name.Split('(');
                    int.TryParse(nameArray[1].Replace("(", "").Replace(")", ""), out var charges);
                    if (charges != 1)
                    {
                        character.SendChatMessage("Your " + nameArray[0] + " has " + (charges - 1) + " charges remaining.");
                        var newItem = character.ServiceProvider.GetRequiredService<IItemBuilder>()
                            .Create()
                            .WithId(jewelry.Id + 1)
                            .WithCount(jewelry.Count)
                            .Build();
                        if (equipment) character.Equipment.Replace((EquipmentSlot)slot, newItem);
                        else character.Inventory.Replace(slot, newItem);
                    }
                    else
                    {
                        character.SendChatMessage("Your " + nameArray[0] + " has been depleted of all its charges.");
                        if (equipment) character.Equipment.Remove(jewelry, (EquipmentSlot)slot);
                        else character.Inventory.Remove(jewelry, slot);
                    }

                    return true;
                }).PerformTeleport(character);
    }
}