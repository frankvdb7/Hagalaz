using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Widgets.Shop
{
    /// <summary>
    ///     The item information tab.
    /// </summary>
    public class ItemInfoScript : WidgetScript
    {
        public ItemInfoScript(ICharacterContextAccessor characterContextAccessor, IItemService itemService) : base(characterContextAccessor) => _itemRepository = itemService;

        /// <summary>
        ///     The selected item
        /// </summary>
        public IItem SelectedItem { get; set; }

        /// <summary>
        ///     The sample item
        /// </summary>
        public bool SampleItem { get; set;  }

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemRepository;

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            if (SampleItem)
            {
                Owner.SendChatMessage(SelectedItem.Name + ": free sample!");
            }
            else
            {
                Owner.SendChatMessage(SelectedItem.Name + ": currently costs " + Owner.CurrentShop.GetBuyValue(SelectedItem) + " " + _itemRepository.FindItemDefinitionById(Owner.CurrentShop.CurrencyId).Name.ToLower() + ".");
            }

            Setup();
        }

        /// <summary>
        ///     Set's configurations.
        /// </summary>
        public void Setup()
        {
            //this.interfaceInstance.SetOptions(21, 0, 0, 0); // Item info
            //this.interfaceInstance.SetOptions(15, 0, 0, 4); // Enables buy / take button.

            Owner.Configurations.SendGlobalCs2Int(741, SelectedItem.Id);
            Owner.Configurations.SendGlobalCs2Int(743, Owner.CurrentShop.CurrencyId); // currency
            Owner.Configurations.SendGlobalCs2Int(744, SampleItem ? -1 : Owner.CurrentShop.GetBuyValue(SelectedItem));
            //this.owner.Configurations.SendGlobalCS2Int(745, 0); // ???
            //this.owner.Configurations.SendGlobalCS2Int(746, -1); // ???
            //this.owner.Configurations.SendGlobalCS2Int(168, 98); // ???
            Owner.Configurations.SendGlobalCs2String(25, SelectedItem.ItemScript.GetExamine(SelectedItem));
            //this.owner.Configurations.SendGlobalCS2String(34, ""); // Something to do with quest id's.
            /*if (item.EquipmentDefinition.Bonuses != null)
            {
                string requirementsText = "";
                if (item.EquipmentDefinition.Requirements.Count > 0)
                {
                    foreach (byte skillId in item.EquipmentDefinition.Requirements.Keys)
                    {
                        byte level = item.EquipmentDefinition.Requirements[skillId];
                        if (level <= 0)
                            continue;
                        bool hasRequirement = item.EquipmentDefinition.HasLevelRequirement(skillId, this.owner.Statistics.LevelForExperience(skillId));
                        requirementsText += "<br>" + (hasRequirement ? "<col=00ff00>" : "<col=ff0000>") + " Level " + level + " " + Statistics.SKILL_NAMES[skillId];
                    }
                    this.owner.Configurations.SendGlobalCS2String(26, "Worn on yourself, requiring " + requirementsText);
                }
                else
                {
                    this.owner.Configurations.SendGlobalCS2String(26, "Worn on yourself");
                }
                StringBuilder bonuses = new StringBuilder();
                bonuses.Append("<br>Attack<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.AttackStab));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.AttackSlash));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.AttackCrush));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.AttackMagic));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.AttackRanged));
                this.owner.Configurations.SendGlobalCS2String(35, bonuses.ToString());

                this.owner.Configurations.SendGlobalCS2String(36, "<br><br>Stab<br>Slash<br>Crush<br>Magic<br>Ranged<br>Summoning<br>Strength<br>Ranged Strength<br>Prayer<br>Magic Damage<br>Absorb Melee<br>Absorb Magic<br>Absorb Ranged"); 

                bonuses.Clear();
                bonuses.Append("<<br>Defence<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.DefenceStab));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.DefenceSlash));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.DefenceCrush));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.DefenceMagic));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.DefenceRanged));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.DefenceSummoning));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.Strength));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.RangedStrength));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.Prayer));
                bonuses.Append("<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.MagicDamage));
                bonuses.Append("%<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.AbsorbMelee));
                bonuses.Append("%<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.AbsorbMagic));
                bonuses.Append("%<br><col=ffff00>+");
                bonuses.Append(item.EquipmentDefinition.Bonuses.GetBonus(BonusType.AbsorbRange));
                bonuses.Append("%");
                this.owner.Configurations.SendGlobalCS2String(52, bonuses.ToString());
            }
            else
            {
                this.owner.Configurations.SendGlobalCS2String(26, ""); 
            }*/

            InterfaceInstance.AttachClickHandler(1, (componentID, type, button1, button2) =>
            {
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return true;
            });

            InterfaceInstance.AttachClickHandler(21, (componentID, type, button1, button2) =>
            {
                if (Owner.HasState<ShopSellScreenState>())
                {
                    Owner.CurrentShop.MainStockContainer.SellFromInventory(Owner, SelectedItem, 1);
                }
                else
                {
                    if (SampleItem)
                    {
                        Owner.CurrentShop.SampleStockContainer.BuyFromShop(Owner, SelectedItem, 1);
                    }
                    else if (!Owner.HasState<ShopSellScreenState>())
                    {
                        Owner.CurrentShop.MainStockContainer.BuyFromShop(Owner, SelectedItem, 1);
                    }
                }

                Owner.Configurations.SendGlobalCs2Int(741, SelectedItem.Id); // Refresh remaining money.
                return true;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            //if (this.shopScreen != null)
            //this.shopScreen.SetupInventoryOverlay();
        }
    }
}