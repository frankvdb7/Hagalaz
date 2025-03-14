﻿using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Familiars
{
    [NpcScriptMetaData([6829, 6830])]
    public class SpiritWolf : FamiliarScriptBase
    {
        public SpiritWolf(ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService) : base(pathFinder, npcService, itemService) { }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void InitializeFamiliar() { }

        /// <summary>
        ///     Performs the special attack.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformSpecialMove(IRuneObject target)
        {
            if (!CheckPerformSpecialMove())
            {
                SetUsingSpecialMove(false);
                return;
            }

            base.PerformSpecialMove(target);
            // TODO - Move the target X away.
        }

        /// <summary>
        ///     Sets the special move target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void SetSpecialMoveTarget(IRuneObject? target)
        {
            if (target is ICreature)
            {
                if (SpecialMoveClicked())
                {
                    PerformSpecialMove(target);
                }
            }
        }

        /// <summary>
        ///     Get's amount of special energy required by this FamiliarScript.
        ///     By default , this method does throw NotImplementedException
        /// </summary>
        /// <returns>
        ///     System.Int16.
        /// </returns>
        public override int GetRequiredSpecialMovePoints() => 3;

        /// <summary>
        ///     Gets the type of the special.
        /// </summary>
        /// <returns>
        ///     SpecialType
        /// </returns>
        public override FamiliarSpecialType GetSpecialType() => FamiliarSpecialType.Creature;

        /// <summary>
        ///     Gets the name of the special attack.
        /// </summary>
        /// <returns>
        ///     The special attack name
        /// </returns>
        public override string GetSpecialMoveName() => "Howl";

        /// <summary>
        ///     Gets the special attack description.
        /// </summary>
        /// <returns>
        ///     The special attack description
        /// </returns>
        public override string GetSpecialMoveDescription() =>
            "Scares non-player opponents, causing them to retreat. However, this lasts for only a few seconds.";
    }
}