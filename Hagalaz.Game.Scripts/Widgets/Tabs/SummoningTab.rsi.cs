using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    /// </summary>
    public class SummoningTab : WidgetScript
    {
        public SummoningTab(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {

        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            // if pet
            // toggle component 71 true 
            // toggle component 72 false
            // config 1175 pet hunger %  and pet size %

            Owner.Configurations.SendGlobalCs2Int(168, 8); // set active tab.

            Owner.Configurations.SendStandardConfiguration(1174, Owner.FamiliarScript.Familiar.Appearance.CompositeID); // Could be model id.
            Owner.Configurations.SendStandardConfiguration(1160, 243269632); // NPC emote
            Owner.FamiliarScript.RefreshSpecialMovePoints();
            FamiliarScriptBase.RefreshUsingSpecialAttack();
            Owner.FamiliarScript.RefreshTimer();

            var value = 65536;
            var script = Owner.FamiliarScript;
            if (script.GetSpecialType() == FamiliarSpecialType.Click)
            {
                value = 2;
            }
            else if (script.GetSpecialType() == FamiliarSpecialType.Creature)
            {
                value = 20480;
            }

            InterfaceInstance.SetOptions(74, 0, 0, value);

            /*this.interfaceInstance.SetVisible(44, false);
            this.interfaceInstance.SetVisible(45, false);
            this.interfaceInstance.SetVisible(46, false);
            this.interfaceInstance.SetVisible(47, false);
            this.interfaceInstance.SetVisible(48, false);
            this.interfaceInstance.SetVisible(71, true);
            this.interfaceInstance.SetVisible(72, true);*/

            // Call FamiliarScript
            InterfaceInstance.AttachClickHandler(49, (componentID, clickType, extra1, extra2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                Owner.FamiliarScript.CallFamiliar();
                return true;
            });

            // Dismiss FamiliarScript
            InterfaceInstance.AttachClickHandler(51, (componentID, clickType, extra1, extra2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                new FamiliarDismissEvent(Owner).Send();
                return true;
            });

            // Take BoB
            InterfaceInstance.AttachClickHandler(67, (componentID, clickType, extra1, extra2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (Owner.FamiliarScript is not BobFamiliarScriptBase bob)
                {
                    return false;
                }

                Owner.Inventory.AddAndRemoveFrom(bob.Inventory);
                return true;
            });

            // Renew FamiliarScript
            InterfaceInstance.AttachClickHandler(69, (componentID, clickType, extra1, extra2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                Owner.FamiliarScript.RenewFamiliar();
                return true;
            });

            // Cast special move
            InterfaceInstance.AttachClickHandler(74, (componentID, clickType, extra1, extra2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                Owner.FamiliarScript.SetSpecialMoveTarget(null);
                return true;
            });

            // Attack
            InterfaceInstance.AttachUseOnCreatureHandler(65, (componentID, creature, forceRun, extraData1, extraData2) =>
            {
                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (Owner.FamiliarScript.Familiar == creature)
                {
                    return false;
                }

                Owner.FamiliarScript.Familiar.QueueTask(new RsTask(() => Owner.FamiliarScript.Familiar.Combat.SetTarget(creature), 1));
                return true;
            });

            // Cast special move
            InterfaceInstance.AttachUseOnCreatureHandler(74, (componentID, creature, forceRun, extraData1, extraData2) =>
            {
                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (Owner.FamiliarScript.Familiar == creature)
                {
                    return false;
                }

                Owner.FamiliarScript.SetSpecialMoveTarget(creature);
                return true;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() => Owner.Configurations.SendGlobalCs2Int(168, 4);
    }
}