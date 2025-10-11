using Hagalaz.Game.Abstractions.Logic.Random;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    /// <summary>
    /// Defines a marker interface for any object that can be part of a slayer task generation table.
    /// It inherits from <see cref="IRandomObject"/>, indicating that it can be selected from a weighted random collection.
    /// </summary>
    public interface ISlayerObject : IRandomObject
    {

    }
}