using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Clans
{
    [EquipmentScriptMetaData([20708, 20709])]
    public class ClanEquipment : EquipmentScript
    {
        private readonly IEventManager _eventManager;
        private readonly ICacheAPI _cache;
        private readonly IItemPartFactory _itemPartFactory;
        private readonly IClientMapDefinitionProvider _clientMapDefinitionProvider;

        public ClanEquipment(IEventManager eventManager, ICacheAPI cache, IItemPartFactory itemPartFactory, IClientMapDefinitionProvider clientMapDefinitionProvider)
        {
            _eventManager = eventManager;
            _cache = cache;
            _itemPartFactory = itemPartFactory;
            _clientMapDefinitionProvider = clientMapDefinitionProvider;
        }

        /// <summary>
        ///     Happens when this item is equipped by specific character.
        ///     By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equipped the item.</param>
        public override void OnEquipped(IItem item, ICharacter character)
        {
            EventHappened clanUpdate = null;
            clanUpdate = _eventManager.Listen<ClanSettingsUpdatedEvent>(e =>
            {
                if (e.Clan == character.Clan)
                {
                    RefreshClanAppearance(item, character);
                }

                return false;
            });
            EventHappened equipmentUpdate = null;
            equipmentUpdate = character.RegisterEventHandler<EquipmentChangedEvent>(e =>
            {
                if (character.Equipment[item.Id == 20708 ? EquipmentSlot.Cape : EquipmentSlot.Weapon] == item)
                {
                    return false;
                }

                _eventManager.StopListen<ClanSettingsUpdatedEvent>(clanUpdate);
                character.UnregisterEventHandler<EquipmentChangedEvent>(equipmentUpdate);

                return false;
            });
            RefreshClanAppearance(item, character);
        }

        /// <summary>
        ///     Refreshes the clan appearance.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        private void RefreshClanAppearance(IItem item, ICharacter character)
        {
            if (!character.HasClan())
            {
                return;
            }

            var itemPart = _itemPartFactory.Create(item.Id);

            var colour1 = character.Clan.Settings.MottifColourLeftTop;
            var colour2 = character.Clan.Settings.MottifColourRightBottom;
            var colour3 = character.Clan.Settings.PrimaryClanColour;
            var colour4 = character.Clan.Settings.SecondaryClanColour;

            if (colour1 != -1 && itemPart.ModelColors[0] != colour1)
            {
                itemPart.SetModelPartColor(0, colour1);
            }

            if (colour2 != -1 && itemPart.ModelColors[1] != colour2)
            {
                itemPart.SetModelPartColor(1, colour2);
            }

            if (colour3 != -1 && itemPart.ModelColors[2] != colour3)
            {
                itemPart.SetModelPartColor(2, colour3);
            }

            if (colour4 != -1 && itemPart.ModelColors[3] != colour4)
            {
                itemPart.SetModelPartColor(3, colour4);
            }

            var texture1 = character.Clan.Settings.MottifTop;
            var texture2 = character.Clan.Settings.MottifBottom;

            var definition = _clientMapDefinitionProvider.Get(3685);
            if (definition == null)
            {
                return;
            }
            var newTexture1 = texture1 == 0 ? 0 : definition.GetIntValue(texture1 + 1);
            var newTexture2 = texture2 == 0 ? 0 : definition.GetIntValue(texture2 + 1);

            if (texture1 != 0 && newTexture1 != 0 && itemPart.TextureColors[0] != newTexture1)
            {
                itemPart.SetModelPartTexture(0, definition.GetIntValue(texture1 + 1));
            }

            if (texture2 != 0 && newTexture2 != 0 && itemPart.TextureColors[1] != newTexture2)
            {
                itemPart.SetModelPartTexture(1, definition.GetIntValue(texture2 + 1));
            }

            character.Appearance.DrawItem(item.Id == 20708 ? BodyPart.CapePart : BodyPart.WeaponPart, itemPart);
        }
    }
}