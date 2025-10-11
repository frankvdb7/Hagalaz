using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines a specialized contract for attacker information where the attacker is a creature.
    /// </summary>
    public interface ICreatureAttackerInfo : IAttackerInfo<ICreature>
    {
    }
}
