using Hagalaz.Game.Abstractions.Logic.Random;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="IRandomObject" />
    public interface ISlayerObject : IRandomObject
    {

        /// <summary>
        /// Gets the probability.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        double GetProbability(ICharacter character);
    }
}