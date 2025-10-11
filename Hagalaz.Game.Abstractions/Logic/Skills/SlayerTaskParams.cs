using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Logic.Skills
{
    /// <summary>
    /// Represents the parameters for a slayer task generation request.
    /// </summary>
    /// <param name="Table">The slayer master's task table from which to generate the assignment.</param>
    /// <param name="Character">The character receiving the new slayer task.</param>
    public record SlayerTaskParams(ISlayerMasterTable Table, ICharacter Character);
}