using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Scripts.Model.Maps;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Areas.Wilderness
{
    /// <summary>
    ///     Contains wilderness non-multi and multi script.
    /// </summary>
    [AreaScriptMetaData([1, 2, 7, 10])]
    public class Wilderness : AreaScript
    {
        /// <summary>
        ///     Initializes area script.
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        ///     Determines whether this instance can attack the specified attacker.
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <param name="target">The target.</param>
        /// <returns>
        ///     <c>true</c> if this instance can attack the specified attacker; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanAttack(ICreature attacker, ICreature target)
        {
            var canAttack = base.CanAttack(attacker, target);
            if (canAttack && attacker is ICharacter attack && target is ICharacter targ)
            {
                var wildyLevel = Math.Min(GetWildernessLevel(attack.Location), GetWildernessLevel(targ.Location));
                var levelDifference = Math.Abs((ShouldRenderBaseCombatLevel(attack) ? attack.Statistics.BaseCombatLevel : attack.Statistics.FullCombatLevel) - (ShouldRenderBaseCombatLevel(targ) ? targ.Statistics.BaseCombatLevel : targ.Statistics.FullCombatLevel));
                if (wildyLevel > 0 && wildyLevel < levelDifference)
                {
                    attack.SendChatMessage("You need to move deeper into the wilderness to attack this player.");
                    return false;
                }
            }

            if (canAttack && attacker is INpc attackNpc && attackNpc.TryGetScript<IFamiliarScript>(out var familiarScript) && target is ICharacter targ2)
            {
                var summoner = familiarScript.Summoner;
                var wildyLevel = Math.Min(GetWildernessLevel(attackNpc.Location), GetWildernessLevel(targ2.Location));
                var levelDifference = Math.Abs((ShouldRenderBaseCombatLevel(summoner) ? summoner.Statistics.BaseCombatLevel : summoner.Statistics.FullCombatLevel) - (ShouldRenderBaseCombatLevel(targ2) ? targ2.Statistics.BaseCombatLevel : targ2.Statistics.FullCombatLevel));
                if (wildyLevel > 0 && wildyLevel < levelDifference)
                {
                    return false;
                }
            }

            return canAttack;
        }

        /// <summary>
        ///     Determines whether this instance [can be attacked] the specified victim.
        /// </summary>
        /// <param name="victim">The victim.</param>
        /// <param name="attacker">The attacker.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can be attacked] the specified victim; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanBeAttacked(ICreature victim, ICreature attacker)
        {
            var canAttack = base.CanBeAttacked(victim, attacker);
            if (!canAttack && attacker is ICharacter attack && victim is ICharacter vict)
            {
                var wildyLevel = Math.Min(GetWildernessLevel(attack.Location), GetWildernessLevel(vict.Location));
                var levelDifference = Math.Abs((ShouldRenderBaseCombatLevel(attack) ? attack.Statistics.BaseCombatLevel : attack.Statistics.FullCombatLevel) - (ShouldRenderBaseCombatLevel(vict) ? vict.Statistics.BaseCombatLevel : vict.Statistics.FullCombatLevel));
                if (wildyLevel > 0 && wildyLevel < levelDifference)
                {
                    attack.SendChatMessage("You need to move deeper into the wilderness to attack this player.");
                    return false;
                }
            }

            if (!canAttack && attacker is ICharacter attack1 && victim is INpc npcVict && ((INpc)victim).TryGetScript<IFamiliarScript>(out var script))
            {
                var summoner = script.Summoner;
                var wildyLevel = Math.Min(GetWildernessLevel(attack1.Location), GetWildernessLevel(npcVict.Location));
                var levelDifference = Math.Abs((ShouldRenderBaseCombatLevel(attack1) ? attack1.Statistics.BaseCombatLevel : attack1.Statistics.FullCombatLevel) - (ShouldRenderBaseCombatLevel(summoner) ? summoner.Statistics.BaseCombatLevel : summoner.Statistics.FullCombatLevel));
                if (wildyLevel > 0 && wildyLevel < levelDifference)
                {
                    attack1.SendChatMessage("You need to move deeper into the wilderness to attack this player.");
                    return false;
                }
            }

            return canAttack;
        }

        /// <summary>
        ///     Determines whether this instance [can do standart teleport] the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can do standart teleport] the specified character; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanDoStandardTeleport(ICharacter character)
        {
            if (GetWildernessLevel(character.Location) > 20)
            {
                character.SendChatMessage("You can not use this teleport above level 20 wilderniss!");
                return false;
            }

            return base.CanDoStandardTeleport(character);
        }

        /// <summary>
        ///     Determines whether this instance [can do amulet teleport] the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="item">The item.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can do amulet teleport] the specified character; otherwise, <c>false</c>.
        /// </returns>
        // TODO - Ring of life and glory > 30
        public override bool CanDoItemTeleport(ICharacter character, IItem item) => base.CanDoItemTeleport(character, item);

        /// <summary>
        ///     Shoulds the render base combat level.
        /// </summary>
        /// <returns></returns>
        public override bool ShouldRenderBaseCombatLevel(ICharacter character) => !character.HasFamiliar();

        /// <summary>
        ///     Gets the PvP combat level range.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override int GetPvPCombatLevelRange(ICharacter character)
        {
            if (character.HasFamiliar())
            {
                return base.GetPvPCombatLevelRange(character); // this
            }

            return GetWildernessLevel(character.Location);
        }

        /// <summary>
        ///     Gets the wilderness level.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        public static int GetWildernessLevel(ILocation location)
        {
            if (location.Y > 9900)
            {
                return (location.Y - 9912) / 8 + 1;
            }

            return (location.Y - 3520) / 8 + 1;
        }

        /// <summary>
        ///     Renders the enter area.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void RenderEnterArea(ICreature creature)
        {
            base.RenderEnterArea(creature);
            if (creature is ICharacter character)
            {
                if (character.Widgets.GetOpenWidget(381) == null)
                {
                    var script = character.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                    character.Widgets.OpenWidget(381, character.GameClient.IsScreenFixed ? 51 : 69, 1, script, false);
                    character.Appearance.Refresh(); // Refresh the combat level range and base combat level.
                }
            }
        }

        /// <summary>
        ///     This method get's called when given character exits
        ///     this area.
        ///     The old area, is this scripts area.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void RenderExitArea(ICreature creature)
        {
            if (creature is ICharacter character)
            {
                if (!Equals(character.Area.Script))
                {
                    var toClose = character.Widgets.GetOpenWidget(381);
                    if (toClose != null)
                    {
                        character.Widgets.CloseWidget(toClose);
                    }

                    character.Appearance.Refresh(); // Reset the combat level range and base combat level.
                }
            }
        }

        /// <summary>
        ///     Happens when character enters this area.
        ///     The new area, is this scripts area.
        ///     By default this method performs some checks on wether some things are allowed in the area. (Familiars)
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnCreatureEnterArea(ICreature creature)
        {
            base.OnCreatureEnterArea(creature);
            if (creature is not ICharacter character)
            {
                return;
            }

            if (character.HasFamiliar())
            {
                var familiar = character.FamiliarScript!.Familiar;
                if (familiar.Definition.Id == familiar.Appearance.CompositeID)
                {
                    familiar.Appearance.Transform(familiar.Definition.Id + 1);
                }
            }

            if (!character.HasScript<WildernessScript>())
            {
                character.AddScript<WildernessScript>();
            }
        }

        /// <summary>
        ///     Happens when character exits this area.
        ///     The old area, is this scripts area.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnCreatureExitArea(ICreature creature)
        {
            base.OnCreatureExitArea(creature);
            if (creature is not ICharacter character)
            {
                return;
            }

            if (!Equals(character.Area))
            {
                if (character.HasFamiliar())
                {
                    var familiar = character.FamiliarScript!.Familiar;
                    if (familiar.Definition.Id != familiar.Appearance.CompositeID)
                    {
                        familiar.Appearance.Transform(familiar.Definition.Id);
                    }
                }

                character.TryRemoveScript<WildernessScript>();
            }
        }
    }
}