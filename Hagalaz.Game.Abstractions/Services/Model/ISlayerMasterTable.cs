using Hagalaz.Game.Abstractions.Logic.Random;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    public interface ISlayerMasterTable : IRandomTable<ISlayerTaskDefinition>
    {
        public int Id { get; }
        public int BaseSlayerRewardPoints { get; }
    }
}
