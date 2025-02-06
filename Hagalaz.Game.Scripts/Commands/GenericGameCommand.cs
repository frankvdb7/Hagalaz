using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Commands
{
    /// <summary>
    /// </summary>
    public class GenericGameCommand : IGenericGameCommand
    {
        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        ///     Gets or sets the command function.
        /// </summary>
        /// <value>
        ///     The command function.
        /// </value>
        public Func<ICharacter, string[], Task<bool>>? CommandFunc { get; init; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public async Task Execute(GameCommandArgs args)
        {
            if (CommandFunc != null)
            {
                args.Handled = await CommandFunc.Invoke(args.Character, args.Arguments);
            }
        }

        /// <summary>
        ///     Gets or sets the permission.
        /// </summary>
        /// <value>
        ///     The permission.
        /// </value>
        public Permission Permission { get; init; }
    }
}