using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGameCommand
    {
        /// <summary>
        ///     Gets or sets the name.
        /// </summary>
        /// <value>
        ///     The name.
        /// </value>
        string Name { get; }
        
        /// <summary>
        ///     Gets or sets the permission.
        /// </summary>
        /// <value>
        ///     The permission.
        /// </value>
        Permission Permission { get; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        Task Execute(GameCommandArgs args);
    }
}