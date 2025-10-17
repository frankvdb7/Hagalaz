using Hagalaz.Game.Abstractions.Logic.Random;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Defines the contract for a Slayer master's task assignment table.
    /// </summary>
    public interface ISlayerMasterTable : IRandomTable<ISlayerTaskDefinition>
    {
        /// <summary>
        /// Gets the ID of the Slayer master.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the base number of Slayer points awarded for completing a task from this master.
        /// </summary>
        public int BaseSlayerRewardPoints { get; }
    }
}