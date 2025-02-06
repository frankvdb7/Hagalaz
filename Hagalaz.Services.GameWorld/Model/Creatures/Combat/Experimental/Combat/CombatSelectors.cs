using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat
{
    /// <summary>
    /// 
    /// </summary>
    public static class CombatSelectors
    {
        /// <summary>
        /// Probabilities the selector.
        /// </summary>
        /// <returns></returns>
        public static Func<IList<T>, T> ProbabilitySelector<T>() where T : ICombatRotation =>
            (rotations) =>
            {
                double hitValue = Random.Shared.Next(rotations.Sum(r => r.Probability));
                double runningValue = 0.0;
                foreach (T rotation in rotations)
                {
                    runningValue += rotation.Probability;
                    if (hitValue < runningValue)
                    {
                        return rotation;
                    }
                }

                return default;
            };
    }
}