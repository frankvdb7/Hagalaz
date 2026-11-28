using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena.Interfaces
{
    public class DuelScreenScript : WidgetScript
    {
        public DuelArenaScript? Script { get; set; }
        public Action? CloseCallback { get; set; }

        public DuelScreenScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        public override void OnOpen()
        {
            AttachRuleButton(30, DuelRules.Rule.NoRanged);
            AttachRuleButton(31, DuelRules.Rule.NoMelee);
            AttachRuleButton(32, DuelRules.Rule.NoMagic);
            AttachRuleButton(33, DuelRules.Rule.FunWeapons);
            AttachRuleButton(34, DuelRules.Rule.NoForfeit);
            AttachRuleButton(35, DuelRules.Rule.NoDrinks);
            AttachRuleButton(36, DuelRules.Rule.NoFood);
            AttachRuleButton(37, DuelRules.Rule.NoPrayer);
            AttachRuleButton(38, DuelRules.Rule.NoMovement);
            AttachRuleButton(39, DuelRules.Rule.Obstacles);
            AttachRuleButton(40, DuelRules.Rule.EnableSummoning);
            AttachRuleButton(41, DuelRules.Rule.NoSpecialAttacks);
            AttachRuleButton(18, DuelRules.Rule.NoHelm);
            AttachRuleButton(19, DuelRules.Rule.NoCape);
            AttachRuleButton(20, DuelRules.Rule.NoAmulet);
            AttachRuleButton(28, DuelRules.Rule.NoAmmo);
            AttachRuleButton(21, DuelRules.Rule.NoWeapon);
            AttachRuleButton(22, DuelRules.Rule.NoBody);
            AttachRuleButton(23, DuelRules.Rule.NoShield);
            AttachRuleButton(24, DuelRules.Rule.NoLegs);
            AttachRuleButton(25, DuelRules.Rule.NoGloves);
            AttachRuleButton(26, DuelRules.Rule.NoBoots);
            AttachRuleButton(27, DuelRules.Rule.NoRing);
        }

        private void AttachRuleButton(int component, DuelRules.Rule rule) =>
            InterfaceInstance.AttachClickHandler(component, (_, _, _, _) =>
            {
                if (Script is not { Target: { } target } script)
                {
                    return false;
                }

                script.CurrentRules.ToggleRule(rule);
                script.CurrentRules.SendRules(script.Character, target);
                return true;
            });

        public override void OnClose()
        {
            CloseCallback?.Invoke();
            Script = null;
        }
    }
}
