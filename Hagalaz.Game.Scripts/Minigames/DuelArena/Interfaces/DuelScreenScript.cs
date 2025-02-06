using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena.Interfaces
{
    /// <summary>
    /// </summary>
    public class DuelScreenScript : WidgetScript
    {
        /// <summary>
        ///     The script.
        /// </summary>
        public DuelArenaScript Script { get; set; }

        /// <summary>
        ///     Gets or sets the close callback.
        /// </summary>
        public Action CloseCallback { get; set; }


        public DuelScreenScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.AttachClickHandler(30,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoRanged); // no ranged
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(31,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoMelee); // no melee
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(32,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoMagic); // no magic
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(33,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.FunWeapons);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(34,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoForfeit); // no forfeit
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(35,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoDrinks);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(36,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoFood);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(37,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoPrayer);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(38,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoMovement);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(39,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.Obstacles);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(40,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.EnableSummoning); // enable summoning
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(41,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoSpecialAttacks); // no special attacks
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(18,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoHelm);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(19,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoCape);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(20,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoAmulet);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(28,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoAmmo);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(21,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoWeapon);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(22,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoBody);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(23,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoShield);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(24,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoLegs);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(25,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoGloves);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(26,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoBoots);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
            InterfaceInstance.AttachClickHandler(27,
                (componentId, clickType, extra1, extra2) =>
                {
                    Script.CurrentRules.ToggleRule(DuelRules.Rule.NoRing);
                    Script.CurrentRules.SendRules(Script.Character, Script.Target);
                    return true;
                });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (CloseCallback != null)
            {
                CloseCallback();
            }

            Script = null;
        }
    }
}