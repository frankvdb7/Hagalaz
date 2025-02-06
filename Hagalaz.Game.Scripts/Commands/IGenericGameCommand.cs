using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Commands
{
    public interface IGenericGameCommand : IGameCommand
    {
        /// <summary>
        ///     Gets or sets the command function.
        /// </summary>
        /// <value>
        ///     The command function.
        /// </value>
        Func<ICharacter, string[], Task<bool>>? CommandFunc { get; init; }
    }
}